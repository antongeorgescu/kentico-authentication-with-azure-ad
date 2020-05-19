﻿using CMS;
using CMS.Base;
using CMS.CMSImportExport;
using CMS.Core;
using CMS.DataEngine;
using CMS.EventLog;
using CMS.FormEngine;
using CMS.Globalization;
using CMS.Helpers;
using CMS.IO;
using CMS.MacroEngine;
using CMS.Membership;
using CMS.Modules;
using CMS.PortalEngine;
using CMS.UIControls;
using CMS.URLRewritingEngine;
using CMS.WorkflowEngine;
using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;


[assembly: RegisterModule(typeof(UpgradeModule))]

/// <summary>
/// Updates the application data to a newer version if necessary.
/// </summary>
internal sealed class UpgradeModule : Module
{
    public UpgradeModule()
        : base("CMS.Upgrade")
    {
    }


    protected override void OnPreInit()
    {
        base.OnPreInit();
        ApplicationEvents.UpdateData.Execute += (sender, e) => UpgradeProcedure.Update();
    }
}


/// <summary>
/// Class carrying the code to perform the upgrade procedure.
/// </summary>
internal static class UpgradeProcedure
{
    #region "Variables"

    // Path to the upgrade package
    private static string mUpgradePackagePath;
    private static string mWebsitePath;
    private static string mEventLogSource;

    #endregion


    #region "Properties"

    /// <summary>
    /// Gets the source text for event log records generated by upgrade actions
    /// </summary>
    private static string EventLogSource
    {
        get
        {
            return mEventLogSource ?? (mEventLogSource = string.Format("Upgrade to {0}", CMSVersion.MainVersion));
        }
    }

    #endregion


    #region "Main update method"

    /// <summary>
    /// Runs the update procedure.
    /// </summary>
    public static void Update()
    {
        if (DatabaseHelper.IsDatabaseAvailable && SystemContext.IsCMSRunningAsMainApplication)
        {
            try
            {
                string version = SettingsKeyInfoProvider.GetValue("CMSDataVersion");
                switch (version.ToLowerInvariant())
                {
                    case "11.0":
                        using (var context = new CMSActionContext())
                        {
                            context.LogLicenseWarnings = false;

                            UpgradeApplication(Upgrade110To120, "12.0", "Upgrade_110_120.zip");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                EventLogProvider.LogException(EventLogSource, "UPGRADE", ex);
            }
        }
    }

    #endregion


    #region "General purpose - all versions methods"

    private static void UpgradeApplication(Func<bool> versionSpecificMethod, string newVersion, string packageName)
    {
        // Increase the timeout for upgrade request due to expensive operations like macro signing and conversion (needed for large DBs)
        HttpContext.Current.Server.ScriptTimeout = 14400;

        // CI can't be enabled during (and after) upgrade because repository won't be consistent with actual data on the database
        SettingsKeyInfoProvider.SetGlobalValue("CMSEnableCI", false);

        EventLogProvider.LogInformation(EventLogSource, "START");

        // Set the path to the upgrade package (this has to be done here, not in the Import method, because it's an async procedure without HttpContext)
        mUpgradePackagePath = HttpContext.Current.Server.MapPath("~/CMSSiteUtils/Import/" + packageName);
        mWebsitePath = HttpContext.Current.Server.MapPath("~/");

        // Update class form and alt.form definitions
        var classFormDefinitionUpdateHelper = new ClassFormDefinitionUpdateHelper(EventLogSource);
        classFormDefinitionUpdateHelper.UpdateClassFormDefinitions();

        // Set data version
        SettingsKeyInfoProvider.SetGlobalValue("CMSDataVersion", newVersion);
        SettingsKeyInfoProvider.SetGlobalValue("CMSDBVersion", newVersion);

        // Clear hashtables
        ModuleManager.ClearHashtables();

        // Clear the cache
        CacheHelper.ClearCache(null, true);

        // Drop the routes
        CMSDocumentRouteHelper.DropAllRoutes();

        // Call version specific operations
        if (versionSpecificMethod != null)
        {
            using (var context = new CMSActionContext())
            {
                context.DisableLogging();
                context.CreateVersion = false;
                context.LogIntegration = false;

                versionSpecificMethod.Invoke();
            }
        }

        // Import upgrade package with webparts, widgets...
        UpgradeImportPackage();

        RefreshMacroSignatures();

        EventLogProvider.LogInformation(EventLogSource, "FINISH");
    }


    /// <summary>
    /// Procedures which automatically imports the upgrade export package with all WebParts, Widgets, Reports and TimeZones.
    /// </summary>
    private static void UpgradeImportPackage()
    {
        // Import
        try
        {
            RequestStockHelper.Remove("CurrentDomain", true);

            var importSettings = new SiteImportSettings(MembershipContext.AuthenticatedUser)
            {
                DefaultProcessObjectType = ProcessObjectEnum.All,
                SourceFilePath = mUpgradePackagePath,
                WebsitePath = mWebsitePath
            };

            using (var context = new CMSActionContext())
            {
                context.DisableLogging();
                context.CreateVersion = false;
                context.LogIntegration = false;

                ImportProvider.ImportObjectsData(importSettings);

                // Regenerate time zones
                TimeZoneInfoProvider.GenerateTimeZoneRules();

                // Delete the files for separable modules which are not install and therefore not needed
                DeleteWebPartsOfUninstalledModules();

                ImportMetaFiles(Path.Combine(mWebsitePath, "App_Data\\CMSTemp\\Upgrade"));
            }
        }
        catch (Exception ex)
        {
            EventLogProvider.LogException(EventLogSource, "IMPORT", ex);
        }
    }


    /// <summary>
    /// Refreshes macro signatures in all object which can contain macros.
    /// </summary>
    private static void RefreshMacroSignatures()
    {
        // Get object types
        var objectTypes = new List<string> {
            TransformationInfo.OBJECT_TYPE,
            UIElementInfo.OBJECT_TYPE,
            FormUserControlInfo.OBJECT_TYPE,
            SettingsKeyInfo.OBJECT_TYPE,
            AlternativeFormInfo.OBJECT_TYPE,
            DataClassInfo.OBJECT_TYPE, // Process all data classes just through general object type to avoid duplicities
            PageTemplateInfo.OBJECT_TYPE,
            LayoutInfo.OBJECT_TYPE,
            CssStylesheetInfo.OBJECT_TYPE,
            WorkflowActionInfo.OBJECT_TYPE,
        };

        var adminIdentityOption = MacroIdentityOption.FromUserInfo(UserInfoProvider.AdministratorUser);
        foreach (string type in objectTypes)
        {
            try
            {
                using (var context = new CMSActionContext())
                {
                    context.DisableLogging();
                    context.CreateVersion = false;
                    context.LogIntegration = false;

                    var infos = new InfoObjectCollection(type);
                    foreach (var info in infos)
                    {
                        MacroSecurityProcessor.RefreshSecurityParameters(info, adminIdentityOption, true);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogProvider.LogException(EventLogSource, "REFRESHMACROSIGNATURES", ex, 0, "Type: " + type);
            }
        }
    }


    /// <summary>
    /// Deletes the files for separable modules which are not install and therefore not needed.
    /// </summary>
    private static void DeleteWebPartsOfUninstalledModules()
    {
        var webPartsPath = mWebsitePath + "CMSWebParts\\";
        var files = new List<string>();

        var separableModules = new List<string>
        {
            ModuleName.BIZFORM,
            ModuleName.BLOGS,
            ModuleName.COMMUNITY,
            ModuleName.ECOMMERCE,
            ModuleName.EVENTMANAGER,
            ModuleName.FORUMS,
            ModuleName.MEDIALIBRARY,
            ModuleName.MESSAGEBOARD,
            ModuleName.NEWSLETTER,
            ModuleName.NOTIFICATIONS,
            ModuleName.ONLINEMARKETING,
            ModuleName.POLLS,
            ModuleName.REPORTING,
            ModuleName.STRANDSRECOMMENDER,
            ModuleName.CHAT,
        };

        foreach (var separableModule in separableModules)
        {
            // Add files from this folder to the list of files to delete if the module is not installed
            if (!ModuleEntryManager.IsModuleLoaded(separableModule))
            {
                var folderName = GetWebPartFolderName(separableModule);
                files.AddRange(GetAllFiles(webPartsPath + folderName));
            }
        }

        // Remove web parts for separated modules
        foreach (String file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                EventLogProvider.LogException(EventLogSource, "DELETEWEBPARTS", ex, 0, "File: " + file);
            }
        }
    }


    /// <summary>
    /// Returns list of all files in given folder (recursively, from all subdirectories as well).
    /// </summary>
    /// <param name="folder">Folder to search in</param>
    private static List<String> GetAllFiles(String folder)
    {
        var files = new List<string>();

        if (Directory.Exists(folder))
        {
            files.AddRange(Directory.GetFiles(folder));

            var dirs = Directory.GetDirectories(folder);

            foreach (string dir in dirs)
            {
                files.AddRange(GetAllFiles(dir));
            }
        }

        return files;
    }


    /// <summary>
    /// For given module returns it's folder name within CMSWebParts folder.
    /// </summary>
    /// <param name="moduleName">Name of the module</param>
    /// <returns></returns>
    private static string GetWebPartFolderName(string moduleName)
    {
        // Handle exceptions
        switch (moduleName)
        {
            case ModuleName.BIZFORM:
                return "BizForms";

            case ModuleName.BLOGS:
                return "Blogs";

            case ModuleName.NEWSLETTER:
                return "Newsletters";
        }

        // By default, trim "CMS." prefix from module name which will give us folder name withing CMSWebParts directory
        return moduleName.Substring(4);
    }


    /// <summary>
    /// Imports default metafiles which were changed in the new version.
    /// </summary>
    /// <param name="upgradeFolder">Folder where the generated metafiles.xml file is</param>
    private static void ImportMetaFiles(string upgradeFolder)
    {
        try
        {
            // To get the file use Phobos - Generate files button, Metafile settings.
            // Choose only those object types which had metafiles in previous version and these metafiles changed to the new version.
            String xmlPath = Path.Combine(upgradeFolder, "metafiles.xml");
            if (File.Exists(xmlPath))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(xmlPath);

                XmlNode metaFilesNode = xDoc.SelectSingleNode("MetaFiles");
                if (metaFilesNode == null)
                {
                    return;
                }

                String filesDirectory = Path.Combine(upgradeFolder, "Metafiles");

                using (new CMSActionContext { LogEvents = false })
                {
                    foreach (XmlNode metaFile in metaFilesNode)
                    {
                        // Load metafiles information from XML
                        if (metaFile.Attributes == null)
                        {
                            continue;
                        }

                        String objType = metaFile.Attributes["ObjectType"].Value;
                        String groupName = metaFile.Attributes["GroupName"].Value;
                        String codeName = metaFile.Attributes["CodeName"].Value;
                        String fileName = metaFile.Attributes["FileName"].Value;
                        String extension = metaFile.Attributes["Extension"].Value;
                        String fileGUID = metaFile.Attributes["FileGUID"].Value;
                        String title = (metaFile.Attributes["Title"] != null) ? metaFile.Attributes["Title"].Value : null;
                        String description = (metaFile.Attributes["Description"] != null) ? metaFile.Attributes["Description"].Value : null;

                        // Try to find correspondent info object
                        BaseInfo infoObject = ProviderHelper.GetInfoByName(objType, codeName);
                        if (infoObject == null)
                        {
                            continue;
                        }

                        int infoObjectId = infoObject.Generalized.ObjectID;

                        // Check if metafile exists
                        InfoDataSet<MetaFileInfo> metaFilesSet = MetaFileInfoProvider.GetMetaFilesWithoutBinary(infoObjectId, objType, groupName, "MetaFileGUID = '" + fileGUID + "'", null);
                        if (!DataHelper.DataSourceIsEmpty(metaFilesSet))
                        {
                            continue;
                        }

                        // Create new metafile if does not exists
                        String mfFileName = String.Format("{0}.{1}", fileGUID, extension.TrimStart('.'));
                        MetaFileInfo mfInfo = new MetaFileInfo(Path.Combine(filesDirectory, mfFileName), infoObjectId, objType, groupName);
                        mfInfo.MetaFileGUID = ValidationHelper.GetGuid(fileGUID, Guid.NewGuid());

                        // Set correct properties
                        mfInfo.MetaFileName = fileName;
                        if (title != null)
                        {
                            mfInfo.MetaFileTitle = title;
                        }
                        if (description != null)
                        {
                            mfInfo.MetaFileDescription = description;
                        }

                        // Save new meta file
                        MetaFileInfoProvider.SetMetaFileInfo(mfInfo);
                    }

                    // Remove existing files after successful finish
                    String[] files = Directory.GetFiles(upgradeFolder);
                    foreach (String file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            EventLogProvider.LogException(EventLogSource, "IMPORTMETAFILES", ex);
        }
    }

    #endregion


    #region "Update from 11.0 to 12.0"

    /// <summary>
    /// Handles all the specific operations for upgrade from 11.0 to 12.0.
    /// </summary>
    private static bool Upgrade110To120()
    {
        return true;
    }

    #endregion


    #region "Individual upgrade methods"

    // Individual upgrade methods belong here

    #endregion
}
﻿using CMS.Base;
using CMS.Base.Web.UI;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.EventLog;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.IO;
using CMS.LicenseProvider;
using CMS.Localization;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.UIControls;
using CMS.WorkflowEngine;
using System;
using System.Collections;
using System.Data;
using System.Web;


public partial class CMSModules_Content_Controls_Attachments_DirectFileUploader_DirectFileUploaderControl : DirectFileUploader
{
    #region "Contstants"

    private const int DEFAULT_OBJECT_WIDTH = 300;
    private const int DEFAULT_OBJECT_HEIGHT = 200;

    #endregion


    #region "Variables"

    private bool? mIsSupportedBrowser = null;

    protected TreeNode node = null;
    protected TreeProvider mTreeProvider = null;
    private WorkflowManager mWorkflowManager = null;
    private WorkflowInfo wi = null;
    private VersionManager mVersionManager = null;

    #endregion


    #region "Public properties"

    /// <summary>
    /// Uploaded file.
    /// </summary>
    public override HttpPostedFile PostedFile
    {
        get
        {
            return ucFileUpload.PostedFile;
        }
    }


    /// <summary>
    /// File upload user control.
    /// </summary>
    public override CMSFileUpload FileUploadControl
    {
        get
        {
            return ucFileUpload;
        }
    }


    /// <summary>
    /// Indicates if supported browser.
    /// </summary>
    public bool IsSupportedBrowser
    {
        get
        {
            if (mIsSupportedBrowser == null)
            {
                mIsSupportedBrowser = BrowserHelper.IsIE() || BrowserHelper.IsGecko() || BrowserHelper.IsSafari();
            }
            return mIsSupportedBrowser.Value;
        }
    }

    #endregion


    #region "Protected properties"

    /// <summary>
    /// Indicates if check-in/check-out functionality is automatic
    /// </summary>
    protected bool AutoCheck
    {
        get
        {
            if (node != null)
            {
                // Get workflow info
                wi = WorkflowManager.GetNodeWorkflow(node);

                // Check if the document uses workflow
                if (wi != null)
                {
                    return !wi.UseCheckInCheckOut(SiteContext.CurrentSiteName);
                }
            }
            return false;
        }
    }


    /// <summary>
    /// Gets Workflow manager instance.
    /// </summary>
    protected WorkflowManager WorkflowManager
    {
        get
        {
            return mWorkflowManager ?? (mWorkflowManager = WorkflowManager.GetInstance(TreeProvider));
        }
    }


    /// <summary>
    /// Gets Version manager instance.
    /// </summary>
    protected VersionManager VersionManager
    {
        get
        {
            return mVersionManager ?? (mVersionManager = VersionManager.GetInstance(TreeProvider));
        }
    }


    /// <summary>
    /// Tree provider instance.
    /// </summary>
    protected TreeProvider TreeProvider
    {
        get
        {
            return mTreeProvider ?? (mTreeProvider = new TreeProvider(MembershipContext.AuthenticatedUser));
        }
    }

    #endregion


    #region "Control events and methods"

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!StopProcessing)
        {
            Page.Error += new EventHandler(Page_Error);
        }
    }


    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        LoadData();
    }


    protected void btnHidden_Click(object sender, EventArgs e)
    {
        if (!StopProcessing)
        {
            try
            {
                switch (SourceType)
                {
                    case MediaSourceEnum.Attachment:
                        HandleAttachmentUpload(true);
                        break;

                    case MediaSourceEnum.DocumentAttachments:
                        HandleAttachmentUpload(false);
                        break;

                    case MediaSourceEnum.Content:
                        HandleContentUpload();
                        break;

                    case MediaSourceEnum.PhysicalFile:
                        HandlePhysicalFilesUpload();
                        break;

                    case MediaSourceEnum.MetaFile:
                        HandleMetaFileUpload();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                EventLogProvider.LogException("DIRECTFILEUPLOADER", "UPLOADFILE", ex);
                OnError(e);
            }
        }
    }


    protected void LoadData()
    {
        if (StopProcessing)
        {
            // Do nothing
            Visible = false;
        }
        else
        {
            string containerId = QueryHelper.GetString("containerid", "");

            // Initialize uploader
            ucFileUpload.Attributes.Add("class", "fileUpload");
            ucFileUpload.Attributes.Add("style", "cursor: pointer;");
            ucFileUpload.Attributes.Add("onchange", String.Format("if (typeof(parent.DFU) !== 'undefined') {{ parent.DFU.OnUploadBegin({0}); {1}; }}", ScriptHelper.GetString(containerId), Page.ClientScript.GetPostBackEventReference(btnHidden, null, false)));

            // DFU init script
            string dfuScript = String.Format("if (typeof(parent.DFU) !== 'undefined'){{parent.DFU.init(document.getElementById('{0}'), {1}); window.resize = parent.DFU.init(document.getElementById('{0}'));}}", ucFileUpload.ClientID, ScriptHelper.GetString(containerId));
            ScriptHelper.RegisterStartupScript(this, typeof(string), "DFUScript_" + ucFileUpload.ClientID, dfuScript, true);

            btnHidden.Attributes.Add("style", "display:none;");
        }
    }

    #endregion


    #region "Attachments"

    /// <summary>
    /// Provides operations necessary to create and store new attachment.
    /// </summary>
    private void HandleAttachmentUpload(bool fieldAttachment)
    {
        // New attachment
        DocumentAttachment newAttachment = null;

        string message = string.Empty;
        bool fullRefresh = false;
        bool refreshTree = false;

        try
        {
            // Get the existing document
            if (DocumentID != 0)
            {
                // Get document
                node = DocumentHelper.GetDocument(DocumentID, TreeProvider);
                if (node == null)
                {
                    throw new Exception("Given page doesn't exist!");
                }
            }


            #region "Check permissions"

            if (CheckPermissions)
            {
                CheckNodePermissions(node);
            }

            #endregion


            // Check the allowed extensions
            CheckAllowedExtensions();

            // Standard attachments
            if (DocumentID != 0)
            {
                // Check out the document
                if (AutoCheck)
                {
                    // Get original step Id
                    int originalStepId = node.DocumentWorkflowStepID;

                    // Get current step info
                    WorkflowStepInfo si = WorkflowManager.GetStepInfo(node);
                    if (si != null)
                    {
                        // Decide if full refresh is needed
                        bool automaticPublish = wi.WorkflowAutoPublishChanges;
                        // Document is published or archived or uses automatic publish or step is different than original (document was updated)
                        fullRefresh = si.StepIsPublished || si.StepIsArchived || (automaticPublish && !si.StepIsPublished) || (originalStepId != node.DocumentWorkflowStepID);
                    }

                    using (CMSActionContext ctx = new CMSActionContext() { LogEvents = false })
                    {
                        VersionManager.CheckOut(node, node.IsPublished, true);
                    }
                }

                // Handle field attachment
                if (fieldAttachment)
                {
                    // Extension of CMS file before saving
                    string oldExtension = node.DocumentType;

                    newAttachment = DocumentHelper.AddAttachment(node, AttachmentGUIDColumnName, Guid.Empty, Guid.Empty, ucFileUpload.PostedFile, ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize);
                    // Update attachment field
                    DocumentHelper.UpdateDocument(node, TreeProvider);

                    // Different extension
                    if ((oldExtension != null) && !oldExtension.EqualsCSafe(node.DocumentType, true))
                    {
                        refreshTree = true;
                    }
                }
                // Handle grouped and unsorted attachments
                else
                {
                    // Grouped attachment
                    if (AttachmentGroupGUID != Guid.Empty)
                    {
                        newAttachment = DocumentHelper.AddGroupedAttachment(node, AttachmentGUID, AttachmentGroupGUID, ucFileUpload.PostedFile, ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize);
                    }
                    // Unsorted attachment
                    else
                    {
                        newAttachment = DocumentHelper.AddUnsortedAttachment(node, AttachmentGUID, ucFileUpload.PostedFile, ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize);
                    }

                    // Log synchronization task if not under workflow
                    if (wi == null)
                    {
                        DocumentSynchronizationHelper.LogDocumentChange(node, TaskTypeEnum.UpdateDocument, TreeProvider);
                    }
                }

                // Check in the document
                if (AutoCheck)
                {
                    using (CMSActionContext ctx = new CMSActionContext() { LogEvents = false })
                    {
                        VersionManager.CheckIn(node, null, null);
                    }
                }
            }

            // Temporary attachments
            if (FormGUID != Guid.Empty)
            {
                newAttachment = (DocumentAttachment)AttachmentInfoProvider.AddTemporaryAttachment(FormGUID, AttachmentGUIDColumnName, AttachmentGUID, AttachmentGroupGUID, ucFileUpload.PostedFile, SiteContext.CurrentSiteID, ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize);
            }

            // Ensure properties update
            if ((newAttachment != null) && !InsertMode)
            {
                AttachmentGUID = newAttachment.AttachmentGUID;
            }

            if (newAttachment == null)
            {
                throw new Exception("The attachment hasn't been created since no DocumentID or FormGUID was supplied.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            EventLogProvider.LogException("Content", "UploadAttachment", ex);

            message = ex.Message;
        }
        finally
        {
            string afterSaveScript = string.Empty;

            // Call aftersave javascript if exists
            if (!String.IsNullOrEmpty(AfterSaveJavascript))
            {
                if ((message == string.Empty) && (newAttachment != null))
                {
                    string url = null;
                    string safeName = URLHelper.GetSafeFileName(newAttachment.AttachmentName, SiteContext.CurrentSiteName);
                    if (node != null)
                    {
                        SiteInfo si = SiteInfoProvider.GetSiteInfo(node.NodeSiteID);
                        if (si != null)
                        {
                            bool usePermanent = DocumentURLProvider.UsePermanentUrls(si.SiteName);
                            if (usePermanent)
                            {
                                url = ResolveUrl(AttachmentURLProvider.GetAttachmentUrl(newAttachment.AttachmentGUID, safeName));
                            }
                            else
                            {
                                url = ResolveUrl(AttachmentURLProvider.GetAttachmentUrl(safeName, node.NodeAliasPath));
                            }
                        }
                    }
                    else
                    {
                        url = ResolveUrl(AttachmentURLProvider.GetAttachmentUrl(newAttachment.AttachmentGUID, safeName));
                    }
                    // Calling javascript function with parameters attachments url, name, width, height
                    if (!string.IsNullOrEmpty(AfterSaveJavascript))
                    {
                        Hashtable obj = new Hashtable();
                        if (ImageHelper.IsImage(newAttachment.AttachmentExtension))
                        {
                            obj[DialogParameters.IMG_URL] = url;
                            obj[DialogParameters.IMG_TOOLTIP] = newAttachment.AttachmentName;
                            obj[DialogParameters.IMG_WIDTH] = newAttachment.AttachmentImageWidth;
                            obj[DialogParameters.IMG_HEIGHT] = newAttachment.AttachmentImageHeight;
                        }
                        else if (MediaHelper.IsAudioVideo(newAttachment.AttachmentExtension))
                        {
                            obj[DialogParameters.OBJECT_TYPE] = "audiovideo";
                            obj[DialogParameters.AV_URL] = url;
                            obj[DialogParameters.AV_EXT] = newAttachment.AttachmentExtension;
                            obj[DialogParameters.AV_WIDTH] = DEFAULT_OBJECT_WIDTH;
                            obj[DialogParameters.AV_HEIGHT] = DEFAULT_OBJECT_HEIGHT;
                        }
                        else
                        {
                            obj[DialogParameters.LINK_URL] = url;
                            obj[DialogParameters.LINK_TEXT] = newAttachment.AttachmentName;
                        }

                        // Calling javascript function with parameters attachments url, name, width, height
                        afterSaveScript += ScriptHelper.GetScript(string.Format(@"{5}
                        if (window.{0})
                        {{
                            window.{0}('{1}', '{2}', '{3}', '{4}', obj);
                        }}
                        else if((window.parent != null) && window.parent.{0})
                        {{
                            window.parent.{0}('{1}', '{2}', '{3}', '{4}', obj);
                        }}", AfterSaveJavascript, url, newAttachment.AttachmentName, newAttachment.AttachmentImageWidth, newAttachment.AttachmentImageHeight, CMSDialogHelper.GetDialogItem(obj)));
                    }
                }
                else
                {
                    afterSaveScript += ScriptHelper.GetAlertScript(message);
                }
            }

            // Create attachment info string
            string attachmentInfo = ((newAttachment != null) && (newAttachment.AttachmentGUID != Guid.Empty) && (IncludeNewItemInfo)) ? String.Format("'{0}', ", newAttachment.AttachmentGUID) : "";

            // Ensure message text
            message = TextHelper.EnsureLineEndings(message, " ");

            // Call function to refresh parent window
            afterSaveScript += ScriptHelper.GetScript(String.Format(@"
if ((window.parent != null) && (/parentelemid={0}/i.test(window.location.href)) && (window.parent.InitRefresh_{0} != null)){{ 
    window.parent.InitRefresh_{0}({1}, {2}, {3}, {4});
}}",
                                                                    ParentElemID,
                                                                    ScriptHelper.GetString(message.Trim()),
                                                                    (fullRefresh ? "true" : "false"),
                                                                    (refreshTree ? "true" : "false"),
                                                                    attachmentInfo + (InsertMode ? "'insert'" : "'update'")));

            ScriptHelper.RegisterStartupScript(this, typeof(string), "afterSaveScript_" + ClientID, afterSaveScript);
        }
    }


    /// <summary>
    /// Check permissions.
    /// </summary>
    /// <param name="node">Tree node</param>
    private void CheckNodePermissions(TreeNode node)
    {
        // For new document
        if (FormGUID != Guid.Empty)
        {
            if (NodeParentNodeID == 0)
            {
                throw new Exception(GetString("attach.document.parentmissing"));
            }

            if (!RaiseOnCheckPermissions("Create", this))
            {
                if (!MembershipContext.AuthenticatedUser.IsAuthorizedToCreateNewDocument(NodeParentNodeID, NodeClassName))
                {
                    throw new Exception(GetString("attach.actiondenied"));
                }
            }
        }
        // For existing document
        else if (DocumentID > 0)
        {
            if (!RaiseOnCheckPermissions("Modify", this))
            {
                if (MembershipContext.AuthenticatedUser.IsAuthorizedPerDocument(node, NodePermissionsEnum.Modify) == AuthorizationResultEnum.Denied)
                {
                    throw new Exception(GetString("attach.actiondenied"));
                }
            }
        }
    }

    #endregion


    #region "Content files"

    /// <summary>
    /// Provides operations necessary to create and store new content file.
    /// </summary>
    private void HandleContentUpload()
    {
        string message = string.Empty;
        bool newDocumentCreated = false;

        try
        {
            if (NodeParentNodeID == 0)
            {
                throw new Exception(GetString("dialogs.document.parentmissing"));
            }

            // Check license limitations
            if (!LicenseHelper.LicenseVersionCheck(RequestContext.CurrentDomain, FeatureEnum.Documents, ObjectActionEnum.Insert))
            {
                throw new LicenseException(GetString("cmsdesk.documentslicenselimits"));
            }

            // Check if class exists
            DataClassInfo ci = DataClassInfoProvider.GetDataClassInfo(SystemDocumentTypes.File);
            if (ci == null)
            {
                throw new Exception(string.Format(GetString("dialogs.newfile.classnotfound"), SystemDocumentTypes.File));
            }


            #region "Check permissions"

            // Get the node
            TreeNode parentNode = TreeProvider.SelectSingleNode(NodeParentNodeID, LocalizationContext.PreferredCultureCode, true);
            if (parentNode != null)
            {
                // Check whether node class is allowed on site and parent node
                if (!DocumentHelper.IsDocumentTypeAllowed(parentNode, ci.ClassID) || (ClassSiteInfoProvider.GetClassSiteInfo(ci.ClassID, parentNode.NodeSiteID) == null))
                {
                    throw new Exception(GetString("Content.ChildClassNotAllowed"));
                }
            }

            // Check user permissions
            if (!RaiseOnCheckPermissions("Create", this))
            {
                if (!MembershipContext.AuthenticatedUser.IsAuthorizedToCreateNewDocument(parentNode, SystemDocumentTypes.File))
                {
                    throw new Exception(string.Format(GetString("dialogs.newfile.notallowed"), NodeClassName));
                }
            }

            #endregion


            // Check the allowed extensions
            CheckAllowedExtensions();

            // Create new document
            string fileName = Path.GetFileNameWithoutExtension(ucFileUpload.FileName);
            string ext = Path.GetExtension(ucFileUpload.FileName);

            if (IncludeExtension)
            {
                fileName += ext;
            }

            // Make sure the file name with extension respects maximum file name
            fileName = TreePathUtils.EnsureMaxFileNameLength(fileName, ci.ClassName);

            node = TreeNode.New(SystemDocumentTypes.File, TreeProvider);
            node.DocumentCulture = LocalizationContext.PreferredCultureCode;
            node.DocumentName = fileName;
            if (NodeGroupID > 0)
            {
                node.SetValue("NodeGroupID", NodeGroupID);
            }

            // Load default values
            FormHelper.LoadDefaultValues(node.NodeClassName, node);
            node.SetValue("FileDescription", "");
            node.SetValue("FileName", fileName);
            node.SetValue("FileAttachment", Guid.Empty);

            // Set default template ID    
            node.SetDefaultPageTemplateID(ci.ClassDefaultPageTemplateID);

            // Insert the document
            DocumentHelper.InsertDocument(node, parentNode, TreeProvider);

            newDocumentCreated = true;

            // Add the file
            DocumentHelper.AddAttachment(node, "FileAttachment", ucFileUpload.PostedFile, ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize);

            // Update the document
            DocumentHelper.UpdateDocument(node, TreeProvider);

            // Get workflow info
            wi = node.GetWorkflow();

            // Check if auto publish changes is allowed
            if ((wi != null) && wi.WorkflowAutoPublishChanges && !wi.UseCheckInCheckOut(SiteContext.CurrentSiteName))
            {
                // Automatically publish document
                node.MoveToPublishedStep(null);
            }
        }
        catch (Exception ex)
        {
            // Delete the document if something failed
            if (newDocumentCreated && (node != null) && (node.DocumentID > 0))
            {
                DocumentHelper.DeleteDocument(node, TreeProvider, false, true);
            }

            message = ex.Message;
        }
        finally
        {
            // Create node info string
            string nodeInfo = "";
            if ((node != null) && (node.NodeID > 0) && (IncludeNewItemInfo))
            {
                nodeInfo = node.NodeID.ToString();
            }

            // Ensure message text
            message = TextHelper.EnsureLineEndings(message, " ");

            string containerId = QueryHelper.GetString("containerid", "");

            // Call function to refresh parent window  
            string afterSaveScript = null;
            if (!string.IsNullOrEmpty(AfterSaveJavascript))
            {
                afterSaveScript = String.Format(
@"
if (window.{0} != null){{
    window.{0}(files);
}} else if ((window.parent != null) && (window.parent.{0} != null)){{
    window.parent.{0}(files);
}}",
                    AfterSaveJavascript
                );
            }

            afterSaveScript += String.Format(
@"
if (typeof(parent.DFU) !== 'undefined') {{ 
    parent.DFU.OnUploadCompleted({0}); 
}} 
if ((window.parent != null) && (/parentelemid={1}/i.test(window.location.href)) && (window.parent.InitRefresh_{1} != null)){{
    window.parent.InitRefresh_{1}({2}, false, false{3}{4});
}}
",
                ScriptHelper.GetString(containerId),
                ParentElemID,
                ScriptHelper.GetString(message.Trim()),
                ((nodeInfo != "") ? ", '" + nodeInfo + "'" : ""),
                (InsertMode ? ", 'insert'" : ", 'update'")
            );

            ScriptHelper.RegisterStartupScript(this, typeof(string), "afterSaveScript_" + ClientID, ScriptHelper.GetScript(afterSaveScript));
        }
    }

    #endregion


    #region "Physical files"

    /// <summary>
    /// Provides operations necessary to create and store new physical file.
    /// </summary>
    private void HandlePhysicalFilesUpload()
    {
        string message = string.Empty;

        try
        {
            // Check the allowed extensions
            CheckAllowedExtensions();

            // Prepare the file name
            string extension = Path.GetExtension(ucFileUpload.FileName);
            string fileName = TargetFileName;
            if (String.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(ucFileUpload.FileName);
            }
            else if (!fileName.Contains("."))
            {
                fileName += extension;
            }

            // Prepare the path
            if (String.IsNullOrEmpty(TargetFolderPath))
            {
                TargetFolderPath = "~/";
            }

            string filePath = TargetFolderPath;

            // Try to map virtual and relative path to server
            try
            {
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Server.MapPath(filePath);
                }
            }
            catch
            {
            }

            filePath = DirectoryHelper.CombinePath(filePath, fileName);

            // Ensure directory
            DirectoryHelper.EnsureDiskPath(filePath, SystemContext.WebApplicationPhysicalPath);

            // Ensure unique file name
            if (String.IsNullOrEmpty(TargetFileName) && File.Exists(filePath))
            {
                int index = 0;
                string basePath = filePath.Substring(0, filePath.Length - extension.Length);
                string newPath = filePath;

                do
                {
                    index++;
                    newPath = basePath + "_" + index + extension;
                } while (File.Exists(newPath));

                filePath = newPath;
            }

            // Upload file
            if (ImageHelper.IsImage(extension) && ((ResizeToHeight > 0) || (ResizeToWidth > 0) || (ResizeToMaxSideSize > 0)))
            {
                byte[] data = ucFileUpload.FileBytes;

                // Resize the image
                ImageHelper img = new ImageHelper(data);
                int[] newSize = ImageHelper.EnsureImageDimensions(ResizeToWidth, ResizeToHeight, ResizeToMaxSideSize, img.ImageWidth, img.ImageHeight);
                if ((newSize[0] != img.ImageWidth) || (newSize[1] != img.ImageHeight))
                {
                    data = img.GetResizedImageData(newSize[0], newSize[1]);
                }

                // Write to file
                File.WriteAllBytes(filePath, data);
            }
            else
            {
                File.WriteAllBytes(filePath, ucFileUpload.FileBytes);
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            EventLogProvider.LogException("Uploader", "UploadPhysicalFile", ex);

            message = ex.Message;
        }
        finally
        {
            string afterSaveScript = string.Empty;
            if (!String.IsNullOrEmpty(message))
            {
                afterSaveScript = "setTimeout(\"alert(" + ScriptHelper.GetString(ScriptHelper.GetString(message), false) + ")\", 1);";
            }
            else
            {
                if (!string.IsNullOrEmpty(AfterSaveJavascript))
                {
                    afterSaveScript = String.Format(
@"
if (window.{0} != null) {{
    window.{0}(files)
}} else if ((window.parent != null) && (window.parent.{0} != null)) {{
    window.parent.{0}(files) 
}}
",
                        AfterSaveJavascript
                    );
                }
                else
                {
                    afterSaveScript += "if ((window.parent != null) && (/parentelemid=" + ParentElemID + "/i.test(window.location.href)) && (window.parent.InitRefresh_" + ParentElemID + " != null)){window.parent.InitRefresh_" + ParentElemID + "(" + ScriptHelper.GetString(message.Trim()) + ", false, false);}";
                }
            }

            afterSaveScript = ScriptHelper.GetScript(afterSaveScript);

            ScriptHelper.RegisterStartupScript(this, typeof(string), "afterSaveScript_" + ClientID, afterSaveScript);
        }
    }

    #endregion


    #region "Meta files"

    private void HandleMetaFileUpload()
    {
        string message = string.Empty;
        MetaFileInfo mfi = null;

        try
        {
            // Check the allowed extensions
            CheckAllowedExtensions();

            if (InsertMode)
            {
                // Create new meta file
                mfi = new MetaFileInfo(FileUploadControl.PostedFile, ObjectID, ObjectType, Category);
                mfi.MetaFileSiteID = SiteID;
            }
            else
            {
                if (MetaFileID > 0)
                {
                    mfi = MetaFileInfoProvider.GetMetaFileInfo(MetaFileID);
                }
                else
                {
                    DataSet ds = MetaFileInfoProvider.GetMetaFilesWithoutBinary(ObjectID, ObjectType, Category, null, null);
                    if (!DataHelper.DataSourceIsEmpty(ds))
                    {
                        mfi = new MetaFileInfo(ds.Tables[0].Rows[0]);
                    }
                }

                if (mfi != null)
                {
                    string fileExt = Path.GetExtension(FileUploadControl.FileName);
                    // Init the MetaFile data
                    mfi.MetaFileName = URLHelper.GetSafeFileName(FileUploadControl.FileName, null);
                    mfi.MetaFileExtension = fileExt;

                    mfi.MetaFileSize = Convert.ToInt32(FileUploadControl.PostedFile.InputStream.Length);
                    mfi.MetaFileMimeType = MimeTypeHelper.GetMimetype(fileExt);
                    mfi.InputStream = FileUploadControl.PostedFile.InputStream;

                    // Set image properties
                    if (ImageHelper.IsImage(mfi.MetaFileExtension))
                    {
                        // Make MetaFile binary load from InputStream
                        mfi.MetaFileBinary = null;
                        ImageHelper ih = new ImageHelper(mfi.MetaFileBinary);
                        mfi.MetaFileImageHeight = ih.ImageHeight;
                        mfi.MetaFileImageWidth = ih.ImageWidth;
                    }
                }
            }

            if (mfi != null)
            {
                // Save file to the database
                MetaFileInfoProvider.SetMetaFileInfo(mfi);
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            EventLogProvider.LogException("Uploader", "UploadMetaFile", ex);

            message = ex.Message;
        }
        finally
        {
            string afterSaveScript = string.Empty;
            if (String.IsNullOrEmpty(message))
            {
                if (!string.IsNullOrEmpty(AfterSaveJavascript))
                {
                    afterSaveScript = String.Format(
@"
if (window.{0} != null) {{
    window.{0}(files)
}} else if ((window.parent != null) && (window.parent.{0} != null)) {{
    window.parent.{0}(files) 
}}
",
                        AfterSaveJavascript
                    );
                }
                else
                {
                    afterSaveScript = String.Format(@"
                        if ((window.parent != null) && (/parentelemid={0}/i.test(window.location.href)) && (window.parent.InitRefresh_{0}))
                        {{
                            window.parent.InitRefresh_{0}('{1}', false, false, {2});
                        }}
                        else {{ 
                            if ('{1}' != '') {{
                                alert('{1}');
                            }}
                        }}", ParentElemID, ScriptHelper.GetString(message.Trim(), false), mfi.MetaFileID.ToString() + (InsertMode ? ",'insert'" : ",'update'"));
                }
            }
            else
            {
                afterSaveScript += ScriptHelper.GetAlertScript(message, false);
            }

            ScriptHelper.RegisterStartupScript(this, typeof(string), "afterSaveScript_" + ClientID, afterSaveScript, true);
        }
    }

    #endregion
}

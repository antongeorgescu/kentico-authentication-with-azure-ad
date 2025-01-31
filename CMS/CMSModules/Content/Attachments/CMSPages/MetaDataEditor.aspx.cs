﻿using CMS.Base.Web.UI;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.SiteProvider;
using CMS.UIControls;
using System;


public partial class CMSModules_Content_Attachments_CMSPages_MetaDataEditor : CMSLiveModalPage
{
    private string mCurrentSiteName;


    /// <summary>
    /// Returns the site name from query string 'sitename' or 'siteid' if present, otherwise SiteContext.CurrentSiteName.
    /// </summary>
    protected new string CurrentSiteName
    {
        get
        {
            if (mCurrentSiteName == null)
            {
                mCurrentSiteName = QueryHelper.GetString("sitename", SiteContext.CurrentSiteName);

                int siteId = QueryHelper.GetInteger("siteid", 0);

                SiteInfo site = SiteInfoProvider.GetSiteInfo(siteId);
                if (site != null)
                {
                    mCurrentSiteName = site.SiteName;
                }
            }
            return mCurrentSiteName;
        }
    }


    protected void Page_Load(object sender, EventArgs e)
    {
        // Initialize modal page
        RegisterEscScript();

        if (QueryHelper.ValidateHash("hash"))
        {
            string title = GetString("general.editmetadata");
            Page.Title = title;
            PageTitle.TitleText = title;
            // Default image
            btnSave.Click += btnSave_Click;

            AddNoCacheTag();

            // Set metadata editor properties
            metaDataEditor.ObjectGuid = QueryHelper.GetGuid("attachmentguid", Guid.Empty);
            metaDataEditor.ObjectType = AttachmentInfo.OBJECT_TYPE;
            metaDataEditor.ExternalControlID = QueryHelper.GetText("clientid", null);
            metaDataEditor.VersionHistoryID = QueryHelper.GetInteger("versionhistoryid", 0);
            metaDataEditor.SiteName = CurrentSiteName;
        }
        else
        {
            // Hide all controls
            metaDataEditor.Visible = false;
            btnSave.Visible = false;

            string url = ResolveUrl(AdministrationUrlHelper.GetErrorPageUrl("dialogs.badhashtitle", "dialogs.badhashtext", true));
            ltlScript.Text = ScriptHelper.GetScript("window.location = '" + url + "';");
        }
    }


    /// <summary>
    /// Save metadata of attachment.
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Argument</param>
    protected void btnSave_Click(object sender, EventArgs e)
    {
        if (metaDataEditor.SaveMetadata())
        {
            ltlScript.Text = ScriptHelper.GetScript("CloseDialog();");
        }
    }
}

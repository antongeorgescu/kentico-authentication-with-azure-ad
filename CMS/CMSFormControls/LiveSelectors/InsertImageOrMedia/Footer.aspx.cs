﻿using CMS.Base.Web.UI;
using CMS.Helpers;
using CMS.UIControls;
using System;


public partial class CMSFormControls_LiveSelectors_InsertImageOrMedia_Footer : CMSLiveModalPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (QueryHelper.ValidateHash("hash"))
        {
            footerElem.InitFromQueryString();
        }
        else
        {
            footerElem.StopProcessing = true;
            footerElem.Visible = false;
            string url = ResolveUrl(AdministrationUrlHelper.GetErrorPageUrl("dialogs.badhashtitle", "dialogs.badhashtext", true));
            ltlScript.Text = ScriptHelper.GetScript("if (window.parent != null) { window.parent.location = '" + url + "' }");
        }
    }
}

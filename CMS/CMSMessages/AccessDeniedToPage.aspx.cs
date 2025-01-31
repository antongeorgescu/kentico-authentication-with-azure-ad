﻿using CMS.Helpers;
using CMS.UIControls;
using System;

public partial class CMSMessages_AccessDeniedToPage : MessagePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Try skip IIS http errors
        Response.TrySkipIisCustomErrors = true;
        // Set unauthorized state
        Response.StatusCode = 403;

        titleElem.TitleText = GetString("AccessDeniedToPage.Header");
        string url = QueryHelper.GetText("returnurl", String.Empty);
        if (url == String.Empty)
        {
            lblInfo.Text = GetString("AccessDeniedToPage.InfoNoPage");
        }
        else
        {
            lblInfo.Text = String.Format(GetString("AccessDeniedToPage.Info"), url);
        }

        lnkBack.Text = GetString("AccessDeniedToPage.Back");
        lnkBack.NavigateUrl = "~/";
    }
}
﻿using CMS.Helpers;
using CMS.Localization;
using CMS.UIControls;
using System;


public partial class CMSModules_PortalEngine_UI_WebParts_WebPartDocumentationPage : MessagePage
{
    protected void Page_Init(object sender, EventArgs e)
    {
        PageTitle.TitleText = GetString("WebPartDocumentDialog.Documentation");
        PageTitle.ShowCloseButton = false;
        PageTitle.ShowFullScreenButton = false;
        ucWebPartDocumentation.PageTemplateID = QueryHelper.GetInteger("templateID", 0);
        ucWebPartDocumentation.AliasPath = QueryHelper.GetString("aliaspath", String.Empty);
        ucWebPartDocumentation.CultureCode = QueryHelper.GetString("culture", LocalizationContext.PreferredCultureCode);
        ucWebPartDocumentation.IsInline = QueryHelper.GetBoolean("Inline", false);
        ucWebPartDocumentation.ZoneID = QueryHelper.GetString("zoneid", String.Empty);
        ucWebPartDocumentation.WidgetID = QueryHelper.GetInteger("widgetId", 0);
        ucWebPartDocumentation.WebpartID = QueryHelper.GetString("webPartId", String.Empty);
        ucWebPartDocumentation.InstanceGUID = QueryHelper.GetGuid("instanceGuid", Guid.Empty);
        ucWebPartDocumentation.DashboardName = QueryHelper.GetString("dashboard", String.Empty);
        ucWebPartDocumentation.DashboardSiteName = QueryHelper.GetString("sitename", String.Empty);
    }
}
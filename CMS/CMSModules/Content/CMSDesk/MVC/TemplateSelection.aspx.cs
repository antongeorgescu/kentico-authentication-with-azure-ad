﻿using CMS.Base.Web.UI;
using CMS.Base.Web.UI.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Internal;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.UIControls;
using System;


public partial class CMSModules_Content_CMSDesk_MVC_TemplateSelection : CMSPage
{
    private const string MODULE_ID = "CMS.PageTemplates/TemplateSelector";
    private const string DEFAULT_IMAGE_FOR_CUSTOM_TEMPLATES = "CMSModules/CMS_PageTemplates/defaulttemplateimage.png";


    protected void Page_Load(object sender, EventArgs e)
    {
        RegisterLocalizationScript();
        RegisterTemplateSelectionScript();
    }


    private void RegisterLocalizationScript()
    {
        var localizationProvider = Service.Resolve<IClientLocalizationProvider>();
        ScriptHelper.RegisterModule(this, "CMS/RegisterClientLocalization", localizationProvider.GetClientLocalization(MODULE_ID));
    }


    private void RegisterTemplateSelectionScript()
    {
        string templatesServiceUrl = GetTemplatesServiceUrl();

        if (String.IsNullOrEmpty(templatesServiceUrl))
        {
            ShowError(ResHelper.GetString("pagetemplatesmvc.selector.cannotretrievetemplates"));
            return;
        }

        ScriptHelper.RegisterModule(this, MODULE_ID, new
        {
            headerActionsDivId = "headerActions",
            defaultTemplatesDivId = "defaultTemplates",
            customTemplatesDivId = "customTemplates",
            noTemplatesDivId = "noTemplates",
            continueButtonId = btnContinue.ClientID,
            serviceUrl = templatesServiceUrl,
            redirectionUrl = GetRedirectionUrl(),
            sitePresentationUrl = GetSitePresentationUrl(),
            defaultImageUrlForCustomTemplates = UIHelper.GetImageUrl(null, DEFAULT_IMAGE_FOR_CUSTOM_TEMPLATES)
        });

        ScriptHelper.RegisterLoader(this);
    }


    private string GetTemplatesServiceUrl()
    {
        var parentNodeId = QueryHelper.GetInteger("parentnodeid", 0);
        int classId = QueryHelper.GetInteger("classid", 0);
        string culture = QueryHelper.GetString("parentculture", null);

        var parentNode = DocumentHelper.GetDocument(parentNodeId, TreeProvider.ALL_CULTURES, Tree);
        var pageTypeInfo = DataClassInfoProvider.GetDataClassInfo(classId);

        if ((parentNode == null)
            || (pageTypeInfo == null)
            || String.IsNullOrEmpty(culture)
            )
        {
            return null;
        }

        var webServiceUrlProvider = new PageTemplateWebServiceUrlProvider(MembershipContext.AuthenticatedUser);
        return webServiceUrlProvider.GetTemplatesEndpointUrl(parentNode, pageTypeInfo.ClassName, culture);
    }


    private string GetSitePresentationUrl()
    {
        return SiteContext.CurrentSite.SitePresentationURL.TrimEnd('/');
    }


    private string GetRedirectionUrl()
    {
        var redirectionUrl = URLHelper.GetAbsoluteUrl("~/CMSModules/Content/CMSDesk/Edit/Edit.aspx");
        return URLHelper.AppendQuery(redirectionUrl, RequestContext.CurrentQueryString);
    }
}

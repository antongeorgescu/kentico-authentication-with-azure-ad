using CMS.Base;
using CMS.SiteProvider;
using CMS.UIControls;
using System;


public partial class CMSModules_UIPersonalization_Pages_Administration_UI_Dialogs : CMSUIPersonalizationPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        SiteID = CurrentUser.CheckPrivilegeLevel(UserPrivilegeLevelEnum.GlobalAdmin) ? 0 : SiteContext.CurrentSiteID;
        editElem.SiteID = SiteID;
        editElem.IsLiveSite = false;
        editElem.HideSiteSelector = (SiteID != 0);
    }
}
using CMS.Core;
using CMS.UIControls;
using System;


[UIElement(ModuleName.ONLINEMARKETING, "OMDashBoard")]
public partial class CMSModules_OnlineMarketing_Pages_Dashboard : DashboardPage
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        cmsDashboard.SetupSiteDashboard();
    }
}
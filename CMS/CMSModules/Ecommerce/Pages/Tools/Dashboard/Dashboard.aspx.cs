using CMS.Core;
using CMS.UIControls;
using System;


[UIElement(ModuleName.ECOMMERCE, "EcommerceDashboard")]
public partial class CMSModules_Ecommerce_Pages_Tools_Dashboard_Dashboard : DashboardPage
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        cmsDashboard.SetupSiteDashboard();
    }
}
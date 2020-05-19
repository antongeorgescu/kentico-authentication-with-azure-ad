using CMS.Core;
using CMS.UIControls;
using System;


[UIElement(ModuleName.CONTENT, "MyDeskDashBoardItem")]
public partial class CMSModules_MyDesk_Dashboard : DashboardPage
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        cmsDashboard.SetupSiteDashboard();
    }
}
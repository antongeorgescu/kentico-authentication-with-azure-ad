﻿using CMS.Core;
using CMS.UIControls;
using System;


[UIElement(ModuleName.ECOMMERCE, "ReportsDashboard")]
public partial class CMSModules_Ecommerce_Pages_Tools_Reports_Dashboard : DashboardPage
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        cmsDashboard.SetupSiteDashboard();
    }
}
﻿using CMS.Base.Web.UI;
using CMS.UIControls;
using System;
using System.Web.UI.WebControls;

public partial class CMSMasterPages_LiveSite_LiveTabsHeader : CMSLiveMasterPage
{
    /// <summary>
    /// Tabs control.
    /// </summary>
    public override UITabs Tabs
    {
        get
        {
            return tabControlElem;
        }
    }


    /// <summary>
    /// PageTitle control.
    /// </summary>
    public override PageTitle Title
    {
        get
        {
            return titleElem;
        }
    }


    /// <summary>
    /// HeaderActions control.
    /// </summary>
    public override HeaderActions HeaderActions
    {
        get
        {
            if (base.HeaderActions != null)
            {
                return base.HeaderActions;
            }
            return actionsElem;
        }
    }


    /// <summary>
    /// Left tabs panel.
    /// </summary>
    public override Panel PanelLeft
    {
        get
        {
            return pnlLeft;
        }
    }


    /// <summary>
    /// Right tabs panel.
    /// </summary>
    public override Panel PanelRight
    {
        get
        {
            return pnlRight;
        }
    }


    /// <summary>
    /// Tab master page doesn't hide page title.
    /// </summary>
    public override bool TabMode
    {
        get
        {
            return false;
        }
    }


    /// <summary>
    /// Panel containing title.
    /// </summary>
    public override Panel PanelTitle
    {
        get
        {
            return pnlTitle;
        }
    }


    protected void Page_Load(object sender, EventArgs e)
    {
        // Hide actions panel if no actions are present and DisplayActionsPanel is false
        if (!DisplayActionsPanel && !actionsElem.IsVisible())
        {
            pnlActions.Visible = false;
        }
    }
}
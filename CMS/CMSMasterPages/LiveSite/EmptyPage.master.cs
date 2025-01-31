﻿using CMS.UIControls;
using System;
using System.Web.UI.WebControls;

public partial class CMSMasterPages_LiveSite_EmptyPage : CMSLiveMasterPage
{
    #region "Properties"

    /// <summary>
    /// Prepared for specifying the additional HEAD elements.
    /// </summary>
    public override Literal HeadElements
    {
        get
        {
            return ltlHeadElements;
        }
        set
        {
            ltlHeadElements = value;
        }
    }


    /// <summary>
    /// Body element control
    /// </summary>
    public override System.Web.UI.HtmlControls.HtmlGenericControl Body
    {
        get
        {
            return bodyElem;
        }
    }


    /// <summary>
    /// Gets the labels container.
    /// </summary>
    public override PlaceHolder PlaceholderLabels
    {
        get
        {
            return plcLabels;
        }
    }

    #endregion


    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        PageStatusContainer = plcStatus;
        bodyElem.Attributes["class"] = mBodyClass;
    }
}
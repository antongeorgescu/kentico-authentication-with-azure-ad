﻿using CMS.Membership;
using CMS.UIControls;
using System;


public partial class CMSModules_Content_CMSDesk_Properties_Advanced_EditableContent_Header : CMSModalPage
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        var user = MembershipContext.AuthenticatedUser;

        // Check 'read' permissions
        if (!user.IsAuthorizedPerResource("CMS.Content", "Read"))
        {
            RedirectToAccessDenied("CMS.Content", "Read");
        }

        // Check UIProfile
        if (!user.IsAuthorizedPerUIElement("CMS.Content", "Properties.General"))
        {
            RedirectToUIElementAccessDenied("CMS.Content", "Properties.General");
        }

        if (!user.IsAuthorizedPerUIElement("CMS.Content", "General.Advanced"))
        {
            RedirectToUIElementAccessDenied("CMS.Content", "General.Advanced");
        }
    }


    protected void Page_Load(object sender, EventArgs e)
    {
        // Initializes page title control
        PageTitle.TitleText = GetString("EditableContent.Header");
    }
}
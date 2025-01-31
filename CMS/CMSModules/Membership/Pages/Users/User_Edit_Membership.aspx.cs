﻿using CMS.Base;
using CMS.Base.Web.UI;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.UIControls;
using System;
using System.Data;
using System.Linq;

public partial class CMSModules_Membership_Pages_Users_User_Edit_Membership : CMSUsersPage
{
    #region "Variables

    private int mUserID;
    private string currentValues = String.Empty;
    protected UserInfo ui = null;

    #endregion


    #region "Properties"

    /// <summary>
    /// Current user ID.
    /// </summary>
    private int UserID
    {
        get
        {
            if (mUserID == 0)
            {
                mUserID = QueryHelper.GetInteger("userid", 0);
            }

            return mUserID;
        }
    }

    #endregion


    #region "Methods"

    protected void Page_Load(object sender, EventArgs e)
    {
        var user = MembershipContext.AuthenticatedUser;
        // Check UI profile for membership
        if (!user.IsAuthorizedPerUIElement("CMS.Users", "CmsDesk.Membership"))
        {
            RedirectToUIElementAccessDenied("CMS.Users", "CmsDesk.Membership");
        }

        // Check "read" permission
        if (!user.IsAuthorizedPerResource("CMS.Membership", "Read"))
        {
            RedirectToAccessDenied("CMS.Membership", "Read");
        }

        ScriptHelper.RegisterJQuery(Page);
        ui = UserInfoProvider.GetUserInfo(UserID);
        CheckUserAvaibleOnSite(ui);
        EditedObject = ui;

        if (!CheckGlobalAdminEdit(ui))
        {
            plcTable.Visible = false;
            ShowError(GetString("Administration-User_List.ErrorGlobalAdmin"));
            return;
        }

        if ((SiteID > 0) && !MembershipContext.AuthenticatedUser.CheckPrivilegeLevel(UserPrivilegeLevelEnum.Admin))
        {
            CurrentMaster.DisplaySiteSelectorPanel = false;
        }
        else
        {
            CurrentMaster.DisplaySiteSelectorPanel = true;
        }

        var data = MembershipUserInfoProvider.GetMembershipUsers().Where("UserID = " + UserID);
        if (data.Any())
        {
            currentValues = TextHelper.Join(";", DataHelper.GetStringValues(data.Tables[0], "MembershipID"));
        }

        if (!RequestHelper.IsPostBack())
        {
            // Set values
            usMemberships.Value = currentValues;
        }

        // Init uni selector
        usMemberships.SelectItemPageUrl = "~/CMSModules/Membership/Pages/Users/User_Edit_Add_Item_Dialog.aspx";
        usMemberships.ListingWhereCondition = "UserID=" + UserID;
        usMemberships.ReturnColumnName = "MembershipID";
        usMemberships.DynamicColumnName = false;
        usMemberships.GridName = "User_Membership_List.xml";
        usMemberships.OnAdditionalDataBound += usMemberships_OnAdditionalDataBound;
        usMemberships.OnSelectionChanged += usMemberships_OnSelectionChanged;
        usMemberships.AdditionalColumns = "ValidTo";
        usMemberships.DialogWindowHeight = 760;

        // Init 
        int siteID = SiteID;
        if (CurrentMaster.DisplaySiteSelectorPanel)
        {
            // Set site selector
            siteSelector.DropDownSingleSelect.AutoPostBack = true;
            siteSelector.AllowAll = false;
            siteSelector.AllowEmpty = false;
            siteSelector.AllowGlobal = true;
            // Only sites assigned to user
            siteSelector.UserId = UserID;
            siteSelector.OnlyRunningSites = false;
            siteSelector.UniSelector.OnSelectionChanged += new EventHandler(UniSelector_OnSelectionChanged);

            if (!RequestHelper.IsPostBack())
            {
                siteID = SiteContext.CurrentSiteID;

                // If user is member of current site
                if (UserSiteInfoProvider.GetUserSiteInfo(UserID, siteID) != null)
                {
                    // Force uniselector to preselect current site
                    siteSelector.Value = siteID;
                }
            }

            siteID = siteSelector.SiteID;
        }

        string siteWhere = (siteID <= 0) ? "MembershipSiteID IS NULL" : "MembershipSiteID =" + siteID;
        usMemberships.ListingWhereCondition = SqlHelper.AddWhereCondition(usMemberships.ListingWhereCondition, siteWhere);
        usMemberships.WhereCondition = SqlHelper.AddWhereCondition(usMemberships.WhereCondition, siteWhere);

        string script = "function setNewDateTime(date) {$cmsj('#" + hdnDate.ClientID + "').val(date);}";
        ScriptHelper.RegisterClientScriptBlock(Page, typeof(string), "NewDateUniSelectorScript", ScriptHelper.GetScript(script));

        // Manage single item valid to change by calendar
        string eventTarget = Request[postEventSourceID];
        string eventArgument = Request[postEventArgumentID];
        if (eventTarget == ucCalendar.DateTimeTextBox.UniqueID)
        {
            // Check "modify" permission
            if (!MembershipContext.AuthenticatedUser.IsAuthorizedPerResource("CMS.Membership", "ManageUserMembership"))
            {
                RedirectToAccessDenied("CMS.Membership", "Manage user membership");
            }

            int id = ValidationHelper.GetInteger(hdnDate.Value, 0);

            if (id != 0)
            {
                DateTime dt = ValidationHelper.GetDateTime(eventArgument, DateTimeHelper.ZERO_TIME);
                MembershipUserInfo mi = MembershipUserInfoProvider.GetMembershipUserInfo(id, UserID);
                if (mi != null)
                {
                    mi.ValidTo = dt;
                    MembershipUserInfoProvider.SetMembershipUserInfo(mi);

                    // Invalidate changes                        
                    if (ui != null)
                    {
                        ui.Generalized.Invalidate(false);
                    }

                    ShowChangesSaved();
                }
            }
        }
    }


    /// Handles site selection change event
    /// </summary>
    protected void UniSelector_OnSelectionChanged(object sender, EventArgs e)
    {
        pnlUpdate.Update();
    }


    /// <summary>
    /// Callback event for create calendar icon.
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="sourceName">Event source name</param>
    /// <param name="parameter">Event parameter</param>
    /// <param name="val">Value from basic external data bound event</param>
    private object usMemberships_OnAdditionalDataBound(object sender, string sourceName, object parameter, object val)
    {
        switch (sourceName.ToLowerCSafe())
        {
            case "calendar":
                DataRowView drv = (parameter as DataRowView);
                string itemID = drv[usMemberships.ReturnColumnName].ToString();
                string date = drv["ValidTo"].ToString();

                string iconID = "icon_" + itemID;
                string postback = ControlsHelper.GetPostBackEventReference(ucCalendar.DateTimeTextBox, "#").Replace("'#'", "$cmsj('#" + ucCalendar.DateTimeTextBox.ClientID + "').val()");
                string onClick = String.Empty;

                ucCalendar.DateTimeTextBox.Attributes["OnChange"] = postback;

                if (!ucCalendar.UseCustomCalendar)
                {
                    onClick = "$cmsj('#" + hdnDate.ClientID + "').val('" + itemID + "');" + ucCalendar.GenerateNonCustomCalendarImageEvent();
                }
                else
                {
                    onClick = "$cmsj('#" + hdnDate.ClientID + "').val('" + itemID + "'); var dt = $cmsj('#" + ucCalendar.DateTimeTextBox.ClientID + "'); dt.val('" + date + "'); dt.cmsdatepicker('setLocation','" + iconID + "'); dt.cmsdatepicker('show');";
                }

                var button = new CMSGridActionButton
                {
                    ToolTip = GetString("membership.changevalidity"),
                    IconCssClass = "icon-calendar",
                    OnClientClick = onClick + "return false;",
                    ID = iconID
                };

                val = button.GetRenderedHTML();

                break;
        }

        return val;
    }


    protected void usMemberships_OnSelectionChanged(object sender, EventArgs ea)
    {
        SaveData();
    }


    /// <summary>
    /// Check whether current user is allowed to modify another user.
    /// </summary>
    /// <param name="userId">Modified user</param>
    /// <returns>"" or error message.</returns>
    protected static string ValidateGlobalAndDeskAdmin(UserInfo ui)
    {
        string result = String.Empty;

        if (MembershipContext.AuthenticatedUser.CheckPrivilegeLevel(UserPrivilegeLevelEnum.Admin))
        {
            return result;
        }

        if (ui == null)
        {
            result = GetString("Administration-User.WrongUserId");
        }
        else
        {
            if (ui.CheckPrivilegeLevel(UserPrivilegeLevelEnum.Admin))
            {
                result = GetString("Administration-User.NotAllowedToModify");
            }
        }
        return result;
    }


    protected override void OnPreRender(EventArgs e)
    {
        if (RequestHelper.IsPostBack())
        {
            pnlBasic.Update();
        }

        base.OnPreRender(e);
    }


    /// <summary>
    /// Store selected (unselected) roles.
    /// </summary>
    private void SaveData()
    {
        bool updateUser = false;

        // Check permission for manage membership for users
        if (!MembershipContext.AuthenticatedUser.IsAuthorizedPerResource("CMS.Membership", "ManageUserMembership"))
        {
            RedirectToAccessDenied("CMS.Membership", "Manage user membership");
        }

        string result = ValidateGlobalAndDeskAdmin(ui);
        if (result != String.Empty)
        {
            ShowError(result);
            return;
        }

        // Remove old items
        string newValues = ValidationHelper.GetString(usMemberships.Value, null);
        string items = DataHelper.GetNewItemsInList(newValues, currentValues);
        if (!String.IsNullOrEmpty(items))
        {
            string[] newItems = items.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in newItems)
            {
                int membershipID = ValidationHelper.GetInteger(item, 0);
                MembershipUserInfoProvider.RemoveMembershipFromUser(membershipID, UserID);
                updateUser = true;
            }
        }


        // Add new items
        items = DataHelper.GetNewItemsInList(currentValues, newValues);
        if (!String.IsNullOrEmpty(items))
        {
            string[] newItems = items.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            {
                DateTime dt = ValidationHelper.GetDateTime(hdnDate.Value, DateTimeHelper.ZERO_TIME);

                // Add all new items to membership
                foreach (string item in newItems)
                {
                    int membershipID = ValidationHelper.GetInteger(item, 0);
                    MembershipUserInfoProvider.AddMembershipToUser(membershipID, UserID, dt);
                    updateUser = true;
                }
            }
        }

        if (updateUser)
        {
            usMemberships.Reload(true);

            // Invalidate user object            
            if (ui != null)
            {
                ui.Generalized.Invalidate(false);
            }

            ShowChangesSaved();
        }
    }

    #endregion
}

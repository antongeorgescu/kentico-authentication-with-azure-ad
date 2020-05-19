﻿using CMS.Base.Web.UI.ActionsConfig;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EventLog;
using CMS.Helpers;
using CMS.MacroEngine;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.UIControls;
using System;
using System.Web.UI.WebControls;


public partial class CMSModules_Membership_Pages_Users_User_Edit_Password : CMSUsersPage
{
    #region "Constants"

    private const string GENERATEPASSWORD = "generatepassword";

    #endregion


    #region "Private fields"

    private int mUserID;
    private UserInfo mUserInfo;

    #endregion


    #region "Private properties"

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


    /// <summary>
    /// Info object of currently edited user.
    /// </summary>
    private UserInfo UserInfo
    {
        get
        {
            return mUserInfo ?? (mUserInfo = UserInfoProvider.GetUserInfo(UserID));
        }
    }

    #endregion


    protected void Page_Load(object sender, EventArgs e)
    {
        UserInfo ui = UserInfo;

        EditedObject = ui;

        CheckUserAvaibleOnSite(ui);

        // Check that only global administrator can edit global administrator's accounts
        if (!CheckGlobalAdminEdit(ui))
        {
            plcTable.Visible = false;
            ShowError(GetString("Administration-User_List.ErrorGlobalAdmin"));
            return;
        }

        if (ui != null)
        {
            if (!ui.CheckPermissions(PermissionsEnum.Modify, SiteContext.CurrentSiteName, MembershipContext.AuthenticatedUser))
            {
                RedirectToAccessDenied(ui.TypeInfo.ModuleName, CMSAdminControl.PERMISSION_MODIFY);
            }

            passStrength.PlaceholderText = "mem.general.changepassword";

            // Hide warning panel after user extended validity of his own password
            if (ui.UserID == MembershipContext.AuthenticatedUser.UserID)
            {
                btnSetPassword.OnClientClick += "window.top.HideWarning()";
            }
        }

        HeaderActions.AddAction(new HeaderAction
        {
            Text = GetString("Administration-User_Edit_Password.gennew"),
            CommandName = GENERATEPASSWORD,
            OnClientClick = GetGeneratePasswordScript()
        });

        HeaderActions.ActionPerformed += HeaderActions_ActionPerformed;

        if (!RequestHelper.IsPostBack())
        {
            ShowInformation(GetString("Administration-User_Edit_Password.NotificationInfo"));
        }
    }


    #region "Event handlers"

    private void HeaderActions_ActionPerformed(object sender, CommandEventArgs e)
    {
        switch (e.CommandName)
        {
            case GENERATEPASSWORD:
                GenerateNewPassword();
                break;
        }
    }


    /// <summary>
    /// Generates new password and sends it to the user.
    /// </summary>
    private void GenerateNewPassword()
    {
        string password = UserInfoProvider.GenerateNewPassword(SiteContext.CurrentSiteName);
        string userName = UserInfoProvider.GetUserNameById(UserID);
        UserInfoProvider.SetPassword(userName, password);

        ShowChangesSaved();

        // Password is included in email, because it was generated by the system
        SendEmail(password);
    }


    /// <summary>
    /// Sets password of current user.
    /// </summary>
    protected void btnSetPassword_Click(object sender, EventArgs e)
    {
        if (UserInfo == null)
        {
            return;
        }

        if (txtConfirmPassword.Text != passStrength.Text)
        {
            ShowError(GetString("Administration-User_Edit_Password.PasswordsDoNotMatch"));
            return;
        }

        if (!passStrength.IsValid())
        {
            ShowError(AuthenticationHelper.GetPolicyViolationMessage(SiteContext.CurrentSiteName));
            return;
        }

        if (!UserInfo.CheckPermissions(PermissionsEnum.Modify, SiteContext.CurrentSiteName, MembershipContext.AuthenticatedUser))
        {
            RedirectToAccessDenied(GetString("general.actiondenied"));
        }

        // Password has been changed
        string password = passStrength.Text;
        UserInfoProvider.SetPassword(UserInfo, password);

        ShowChangesSaved();

        if (SettingsKeyInfoProvider.GetBoolValue(SiteContext.CurrentSiteName + ".CMSSendPasswordResetConfirmation"))
        {
            AuthenticationHelper.SendPasswordResetConfirmation(UserInfo, SiteContext.CurrentSiteName, "USERSAPP", "Membership.PasswordResetConfirmation");
        }
    }

    #endregion


    #region "Private methods"

    /// <summary>
    /// Sends e-mail with password in case the user has specified his email address.
    /// Otherwise, displays the newly generated password to the current user. 
    /// </summary>
    /// <param name="pswd">Password to send</param>
    private void SendEmail(string pswd)
    {
        if (UserInfo == null)
        {
            return;
        }

        // Check whether the 'From' element was specified
        string siteName = SiteContext.CurrentSiteName;
        string emailFrom = SettingsKeyInfoProvider.GetValue("CMSSendPasswordEmailsFrom", siteName);

        if (string.IsNullOrEmpty(emailFrom))
        {
            ShowError(String.Format("{0} {1}", GetString("Administration-User_Edit_Password.PassChangedNotSent"), GetString("Administration-User_Edit_Password.FromMissing")));

            return;
        }

        string emailTo = UserInfo.Email;
        if (!String.IsNullOrEmpty(emailTo))
        {
            EmailMessage em = new EmailMessage
            {
                From = emailFrom,
                Recipients = emailTo,
                Subject = GetString("Administration-User_Edit_Password.NewGen"),
                EmailFormat = EmailFormatEnum.Default
            };

            // Get e-mail template - try to get site specific template if edited user is assigned to current site
            EmailTemplateInfo template = EmailTemplateProvider.GetEmailTemplate("Membership.ChangedPassword", UserInfo.IsInSite(siteName) ? siteName : null);
            if (template != null)
            {
                em.Body = template.TemplateText;

                // Because the password was generated by the system, it's included in the e-mail
                MacroResolver resolver = MembershipResolvers.GetPasswordResolver(UserInfo, pswd);

                try
                {
                    EmailHelper.ResolveMetaFileImages(em, template.TemplateID, EmailTemplateInfo.OBJECT_TYPE, ObjectAttachmentsCategories.TEMPLATE);

                    // Send message immediately (+ resolve macros)
                    EmailSender.SendEmailWithTemplateText(siteName, em, template, resolver, true);

                    // Inform on success
                    ShowConfirmation(GetString("Administration-User_Edit_Password.PasswordsSent") + " " + HTMLHelper.HTMLEncode(emailTo));

                    return;
                }
                catch (Exception ex)
                {
                    EventLogProvider.LogException("PasswordRetrieval", "USERPASSWORD", ex);
                    ShowError("Failed to send the password: " + ex.Message);
                }
            }
            else
            {
                ShowError(GetString("Administration-User_Edit_Password.PassChangedNotSent"));
            }
        }
        else
        {
            ShowConfirmation(String.Format(GetString("Administration-User_Edit_Password.passshow"), pswd), true);
        }
    }


    /// <summary>
    /// A confirmation message is displayed (in case the user has no email address specified)
    /// before a new password is generated for the user.
    /// </summary>
    private string GetGeneratePasswordScript()
    {
        string clientClick = null;

        UserInfo ui = UserInfo;
        if (ui != null)
        {
            if (string.IsNullOrEmpty(ui.Email))
            {
                clientClick = "var flag = confirm('" + GetString("user.showpasswarning") + "');" + ((ui.UserID == MembershipContext.AuthenticatedUser.UserID) ? "if(flag) {window.top.HideWarning();}" : "") + "return flag;";
            }
            // Set hide action if user extend validity of his own account
            else if (ui.UserID == MembershipContext.AuthenticatedUser.UserID)
            {
                clientClick += "window.top.HideWarning()";
            }
        }

        return clientClick;
    }

    #endregion
}
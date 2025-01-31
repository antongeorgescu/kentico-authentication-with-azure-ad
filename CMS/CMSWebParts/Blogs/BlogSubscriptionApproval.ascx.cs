﻿using CMS.Helpers;
using CMS.PortalEngine;
using CMS.PortalEngine.Web.UI;
using System;

public partial class CMSWebParts_Blogs_BlogSubscriptionApproval : CMSAbstractWebPart
{
    #region "Public Properties"

    /// <summary>
    /// Information text
    /// </summary>
    public string ConfirmationInfoText
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("ConfirmationInfoText"), "");
        }
        set
        {
            this.SetValue("ConfirmationInfoText", value);
        }
    }


    /// <summary>
    /// Confirmation text CSS class
    /// </summary>
    public string ConfirmationTextCssClass
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("ConfirmationTextCssClass"), "");
        }
        set
        {
            this.SetValue("ConfirmationTextCssClass", value);
        }
    }


    /// <summary>
    /// Confirmation button text
    /// </summary>
    public string ConfirmationButtonText
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("ConfirmationButtonText"), "");
        }
        set
        {
            this.SetValue("ConfirmationButtonText", value);
        }
    }


    /// <summary>
    /// Confirmation button CSS class
    /// </summary>
    public string ConfirmationButtonCssClass
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("ConfirmationButtonCssClass"), "");
        }
        set
        {
            this.SetValue("ConfirmationButtonCssClass", value);
        }
    }


    /// <summary>
    /// Successful confirmation text
    /// </summary>
    public string SuccessfulConfirmationText
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("SuccessfulConfirmationText"), "Your subscription was confirmed successfully.");
        }
        set
        {
            this.SetValue("SuccessfulConfirmationText", value);
        }
    }


    /// <summary>
    /// Unsuccessful confirmation text
    /// </summary>
    public string UnsuccessfulConfirmationText
    {
        get
        {
            return ValidationHelper.GetString(this.GetValue("UnsuccessfulConfirmationText"), "Subscription confirmation was unsuccessful.");
        }
        set
        {
            this.SetValue("UnsuccessfulConfirmationText", value);
        }
    }

    #endregion


    #region "Methods"

    /// <summary>
    /// Content loaded event handler.
    /// </summary>
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        subscriptionApproval.StopProcessing = (ViewMode != ViewModeEnum.LiveSite);
        subscriptionApproval.StopProcessing = string.IsNullOrEmpty(QueryHelper.GetString("hash", string.Empty));
    }


    /// <summary>
    /// Content loaded event handler.
    /// </summary>
    public override void OnContentLoaded()
    {
        base.OnContentLoaded();
        SetupControl();
    }


    /// <summary>
    /// Initializes the control properties.
    /// </summary>
    protected void SetupControl()
    {
        if (StopProcessing)
        {
            // Stop processing
            subscriptionApproval.StopProcessing = true;
        }
        else
        {
            string subscription = QueryHelper.GetString("blogsubscriptionhash", string.Empty);

            if (!string.IsNullOrEmpty(subscription))
            {
                subscriptionApproval.SuccessfulConfirmationText = SuccessfulConfirmationText;
                subscriptionApproval.UnsuccessfulConfirmationText = UnsuccessfulConfirmationText;
                subscriptionApproval.ConfirmationInfoText = ConfirmationInfoText;
                subscriptionApproval.ConfirmationTextCssClass = ConfirmationTextCssClass;
                subscriptionApproval.ConfirmationButtonText = ConfirmationButtonText;
                subscriptionApproval.ConfirmationButtonCssClass = ConfirmationButtonCssClass;
            }
            else
            {
                Visible = false;
            }
        }
    }

    #endregion
}

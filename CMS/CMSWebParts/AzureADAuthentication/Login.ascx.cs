﻿using CMS.EventLog;
using CMS.Helpers;
using CMS.PortalEngine.Web.UI;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Web;

namespace AzureADAuthentication.WebParts
{
    public partial class Login : CMSAbstractWebPart
    {
        public override void OnContentLoaded()
        {
            base.OnContentLoaded();
            SetupControl();
        }

        public override void ReloadData()
        {
            base.ReloadData();

            SetupControl();
        }

        protected void SetupControl()
        {
            if (StopProcessing) return;

            try
            {
                var returnUrl = string.IsNullOrWhiteSpace(Settings.AzureADAuthenticationSettings.RedirectAfterLoginPageUrl)
                    ? HttpContext.Current.Request.RawUrl
                    : Settings.AzureADAuthenticationSettings.RedirectAfterLoginPageUrl;

                var authContext =
                    new AuthenticationContext(string.Format(Settings.AzureADAuthenticationSettings.AuthorityUrl,
                        Settings.AzureADAuthenticationSettings.TenantId));
                var authorizationUrl = authContext.GetAuthorizationRequestUrlAsync(
                        string.Format(Settings.AzureADAuthenticationSettings.GraphUrl, ""),
                        Settings.AzureADAuthenticationSettings.ClientId,
                        new Uri(URLHelper.GetAbsoluteUrl(Settings.AzureADAuthenticationSettings
                            .AuthenticationRedirectPage)), UserIdentifier.AnyUser, $"state={returnUrl}")
                    .GetAwaiter()
                    .GetResult();
                btnAzureSignIn.NavigateUrl = authorizationUrl.AbsoluteUri.Replace("form_post","query");
            }
            catch (Exception exception)
            {
                EventLogProvider.LogException("AzureActiveDirectory", "Login", exception);
                Visible = false;
            }

            DataBind();
        }
    }
}
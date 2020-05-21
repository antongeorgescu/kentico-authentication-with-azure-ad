using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CMS.Activities.Loggers;
using CMS.DataEngine;
using CMS.EventLog;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzureADAuthentication.Handlers
{
    public class AzureADAuthenticationHandler : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            try
            {
                var credential = new ClientCredential(Settings.AzureADAuthenticationSettings.ClientId,
                    Settings.AzureADAuthenticationSettings.ApplicationKey);

                var authContext =
                    new AuthenticationContext(string.Format(Settings.AzureADAuthenticationSettings.AuthorityUrl,
                        Settings.AzureADAuthenticationSettings.TenantId));
                var code = ValidationHelper.GetString(context.Request.Params["code"], string.Empty);
                var result = await authContext.AcquireTokenByAuthorizationCodeAsync(code,
                    new Uri(context.Request.Url.GetLeftPart(UriPartial.Path)), credential,
                    string.Format(Settings.AzureADAuthenticationSettings.GraphUrl, ""));

                var adClient = new ActiveDirectoryClient(
                    new Uri(string.Format(Settings.AzureADAuthenticationSettings.GraphUrl, result.TenantId)),
                    async () => await GetAppTokenAsync(result.TenantId));

                var adUser = (User) await adClient.Users
                    .Where(x => x.UserPrincipalName.Equals(result.UserInfo.DisplayableId) ||
                                x.OtherMails.Any(y => y.Equals(result.UserInfo.DisplayableId))).Expand(x => x.MemberOf)
                    .ExecuteSingleAsync();

                var user =
                    UserInfoProvider.GetUsers()
                        .Where("AzureADUsername", QueryOperator.Equals, adUser.UserPrincipalName)
                        .TopN(1)
                        .FirstOrDefault();
                var groupsToAdd = adUser.MemberOf.OfType<Group>()
                    .Select(x => x.DisplayName)
                    .Where(x => Settings.AzureADAuthenticationSettings.GroupsToSync.Contains(x));
                var groupsToRemove = Settings.AzureADAuthenticationSettings.GroupsToSync
                    .Where(x => !groupsToAdd.Contains(x));
                if (user == null)
                {
                    user = new CMS.Membership.UserInfo
                    {
                        UserName = adUser.UserPrincipalName,
                        FirstName = adUser.GivenName,
                        LastName = adUser.Surname,
                        FullName = adUser.DisplayName,
                        Email = string.IsNullOrWhiteSpace(adUser.Mail)
                            ? adUser.OtherMails.FirstOrDefault()
                            : adUser.Mail,
                        //None		    0	User has no privilege level
                        //Editor		1	User is able to use administration interface
                        //Admin		    2	User can use all applications except the global applications and functionality
                        //GlobalAdmin	3	User can use all applications and functionality without any exceptions
                        //by default any new user will have Editor privileges restricted through their role permissions
                        SiteIndependentPrivilegeLevel = CMS.Base.UserPrivilegeLevelEnum.Editor
                    };
                    user.SetValue("AzureADUsername", adUser.UserPrincipalName);
                    user.IsExternal = true;
                    user.Enabled = true;
                    UserInfoProvider.SetUserInfo(user);
                    UserInfoProvider.AddUserToSite(user.UserName, SiteContext.CurrentSiteName);

                    foreach (var group in groupsToAdd)
                    {
                        UserInfoProvider.AddUserToRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .TopN(1)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }
                }
                else
                {
                    user.FirstName = adUser.GivenName;
                    user.LastName = adUser.Surname;
                    user.FullName = adUser.DisplayName;
                    user.Email = string.IsNullOrWhiteSpace(adUser.Mail)
                        ? adUser.OtherMails.FirstOrDefault()
                        : adUser.Mail;
                    user.IsExternal = true;
                    UserInfoProvider.SetUserInfo(user);
                    UserInfoProvider.AddUserToSite(user.UserName, SiteContext.CurrentSiteName);
                    foreach (var group in groupsToAdd)
                    {
                        UserInfoProvider.AddUserToRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .TopN(1)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }

                    foreach (var group in groupsToRemove)
                    {
                        UserInfoProvider.RemoveUserFromRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .TopN(1)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }
                }

                AuthenticationHelper.AuthenticateUser(user.UserName, false);
                MembershipActivityLogger.LogLogin(user.UserName);

                var returnUrl = ValidationHelper.GetString(context.Request.Params["state"], string.Empty);
                URLHelper.Redirect(URLHelper.GetAbsoluteUrl(returnUrl));
            }
            catch (Exception exception)
            {
                EventLogProvider.LogException("AzureActiveDirectory", "Login", exception);
            }
        }

        private static async Task<string> GetAppTokenAsync(string tenantId)
        {
            var authenticationContext =
                new AuthenticationContext(string.Format(Settings.AzureADAuthenticationSettings.AuthorityUrl, tenantId), false);
            var clientCred = new ClientCredential(Settings.AzureADAuthenticationSettings.ClientId,
                Settings.AzureADAuthenticationSettings.ApplicationKey);
            var authenticationResult =
                await
                    authenticationContext.AcquireTokenAsync(
                        string.Format(Settings.AzureADAuthenticationSettings.GraphUrl, ""), clientCred);
            return authenticationResult.AccessToken;
        }
    }
}

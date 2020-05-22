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
                ClientCredential credential = new ClientCredential(Constants.AzureActiveDirectory.ClientId,
                    Constants.AzureActiveDirectory.ApplicationKey);


                var authContext =
                    new AuthenticationContext(string.Format(Constants.AzureActiveDirectory.AuthorityUrl,
                        Constants.AzureActiveDirectory.TenantId));
                var code = ValidationHelper.GetString(HttpContext.Current.Request.QueryString["code"], string.Empty);
                AuthenticationResult result =
                    await
                        authContext.AcquireTokenByAuthorizationCodeAsync(code,
                            new Uri(Request.Url.GetLeftPart(UriPartial.Path)), credential,
                            string.Format(Constants.AzureActiveDirectory.GraphUrl, ""));
                var adClient = new ActiveDirectoryClient(
                    new Uri(string.Format(Constants.AzureActiveDirectory.GraphUrl, result.TenantId)),
                    async () => await GetAppTokenAsync(result.TenantId));

                var adUser =
                    (User)
                    await
                        adClient.Users.Where(x => x.UserPrincipalName.Equals(result.UserInfo.DisplayableId))
                            .Expand(x => x.MemberOf)
                            .ExecuteSingleAsync();

                var user =
                    UserInfoProvider.GetUsers()
                        .Where("AzureADUsername", QueryOperator.Equals, adUser.UserPrincipalName)
                        .FirstOrDefault();
                var groupsToAdd = adUser.MemberOf.OfType<Group>()
                    .Select(x => x.DisplayName)
                    .Where(x => Constants.AzureActiveDirectory.GroupsToSync.Contains(x));
                var groupsToRemove = Constants.AzureActiveDirectory.GroupsToSync
                    .Where(x => !groupsToAdd.Contains(x));

                // check if any of the Azure Active Directory groups are matching by name any Kentico roles
                // if not save an error message in ErrorLog and return              
                bool isGroupMatchRole = false;
                foreach (var group in groupsToAdd)
                {
                    var roleInfo = RoleInfoProvider.GetRoles()
                        .OnSite(SiteContext.CurrentSiteID)
                        .Where("RoleDisplayName", QueryOperator.Equals, group).ToList<RoleInfo>();
                    if (roleInfo.Count > 0)
                    {
                        isGroupMatchRole = true;
                        break;
                    }
                }

                if (!isGroupMatchRole)
                {
                    var logerr = $"Attempted login on {DateTime.Now} by user {adUser.UserPrincipalName},[{adUser.DisplayName}] memberOf {groupsToAdd.ToList<string>().Join(",")}";

                    EventLogProvider.LogEvent(EventType.ERROR,
                        "Login user through Azure Active Directory",
                        "AZUREADLOGINFAILURE",
                        eventDescription: logerr);
                    var returnUrlWithError = ValidationHelper.GetString(this.Context.Request.Params["state"], string.Empty);
                    URLHelper.Redirect(URLHelper.GetAbsoluteUrl($"{returnUrlWithError}?logonresult=Failed&firstname={adUser.DisplayName}&lastname={string.Empty}&lastlogoninfo={logerr}"));
                    return;
                }

                if (user == null)
                {
                    user = new CMS.Membership.UserInfo();
                    user.UserName = adUser.UserPrincipalName;
                    user.FirstName = adUser.GivenName;
                    user.LastName = adUser.Surname;
                    user.FullName = adUser.DisplayName;
                    user.Email = adUser.Mail.IfEmpty(adUser.OtherMails.FirstOrDefault());
                    user.SetValue("AzureADUsername", adUser.UserPrincipalName);
                    user.IsExternal = true;

                    //None		    0	User has no privilege level
                    //Editor		1	User is able to use administration interface
                    //Admin		    2	User can use all applications except the global applications and functionality
                    //GlobalAdmin	3	User can use all applications and functionality without any exceptions
                    user.SiteIndependentPrivilegeLevel = CMS.Base.UserPrivilegeLevelEnum.Editor;

                    user.Enabled = true;
                    UserInfoProvider.SetUserInfo(user);
                    UserInfoProvider.AddUserToSite(user.UserName, SiteContext.CurrentSiteName);

                    foreach (var group in groupsToAdd)
                    {
                        UserInfoProvider.AddUserToRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }
                }
                else
                {
                    user.FirstName = adUser.GivenName;
                    user.LastName = adUser.Surname;
                    user.FullName = adUser.DisplayName;
                    user.Email = adUser.Mail.IfEmpty(adUser.OtherMails.FirstOrDefault());
                    user.IsExternal = true;
                    UserInfoProvider.SetUserInfo(user);
                    UserInfoProvider.AddUserToSite(user.UserName, SiteContext.CurrentSiteName);
                    foreach (var group in groupsToAdd)
                    {
                        UserInfoProvider.AddUserToRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }

                    foreach (var group in groupsToRemove)
                    {
                        UserInfoProvider.RemoveUserFromRole(user.UserName,
                            RoleInfoProvider.GetRoles()
                                .OnSite(SiteContext.CurrentSiteID)
                                .Where("RoleDisplayName", QueryOperator.Equals, group)
                                .FirstOrDefault()?.RoleName ?? "", SiteContext.CurrentSiteName);
                    }
                }

                AuthenticationHelper.AuthenticateUser(user.UserName, false);
                MembershipActivityLogger.LogLogin(user.UserName, DocumentContext.CurrentDocument);
                
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

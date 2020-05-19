using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureADAuthentication.Settings
{
    public static class AzureADAuthenticationSettings
    {
        public static string ClientId => SettingsKeyInfoProvider.GetValue("AzureADClientId");
        public static string TenantId => SettingsKeyInfoProvider.GetValue("AzureADTenantId");
        public static string ApplicationKey => SettingsKeyInfoProvider.GetValue("AzureADApplicationKey");
        public static string AuthorityUrl => SettingsKeyInfoProvider.GetValue("AzureADAuthorityUrl");
        public static string GraphUrl => SettingsKeyInfoProvider.GetValue("AzureADGraphUrl");
        public static string AuthenticationRedirectPage => SettingsKeyInfoProvider.GetValue("AzureADAuthenticationRedirectPage");

        private static string RedirectAfterLoginPageNodeGuid => SettingsKeyInfoProvider.GetValue("AzureADRedirectAfterLoginPage");
        public static string RedirectAfterLoginPageUrl
        {
            get
            {
                Guid nodeGuid;
                if (!Guid.TryParse(RedirectAfterLoginPageNodeGuid, out nodeGuid)) return String.Empty;

                var page = DocumentHelper.GetDocuments()
                    .WhereEquals("NodeGuid", nodeGuid)
                    .Published()
                    .TopN(1)
                    .FirstOrDefault();

                return page == null
                    ? String.Empty
                    : URLHelper.ResolveUrl(page.RelativeURL);
            }
        }

        public static List<string> GroupsToSync
            =>
                SettingsKeyInfoProvider.GetValue("AzureADGroupsToSync")
                    .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
    }
}
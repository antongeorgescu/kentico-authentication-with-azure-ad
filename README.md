# Kentico Authentication with Azure Active Directory
Integrate Kentico with Azure Active Directory. This github repository is extracted initially from https://github.com/kate-orlova/azure-ad-auth-in-kentico

The following changes have been applied:

[Missing instructions] Deploy CMS solution as virtual directory (no need to go "web application")
Keep IIS_APPPOOL\DefaultAppPool (.NET 4.0 and managed integration pipe)
[Missing instructions] Build it with x64 due to some very long package names
[Missing instructions] Add an empty AzureADAuthentication.axd to the CMS project (root)

[Bug fix] Update the verb from "*" to "GET" in AzureADAuthentication.axd handler in web.config (called AzureADAuthenticationHandler, in the file with the same name)
[Bug fix] Update the following line in Login.ascx.cs file: btnAzureSignIn.NavigateUrl = authorizationUrl.AbsoluteUri.Replace("form_post","query"); 
[Bug fix] Update graph.windows.net to the newer graph.microsoft.com

For testing purposes please use the following settings:

*** Azure Active Directory ***********************************
Authority URL: "https://login.microsoftonline.com/{0}"
Graph URL: "https://graph.microsoft.com/v1.0/{0}"

Return URI: http://localhost/MSApp/AzureADAuthentication.axd

Client ID: "5d6c194a-708b-4fd1-8e61-60bbdd47943e"
Tenant ID: "e8422127-880e-4288-928e-4ced14423628"
Application Key: _~DO93T.Udoq2gNw_2_n4JxBrG-18q~o9V   

Username: "azor@alviandalabs.onmicrosoft.com" 
Group: "DogsAndCats" 
Password: "Dogs2020!"

*** Kentico Admin: Settings ***********************************
Authentication Redirect Page: "/AzureADAuthentication.axd"





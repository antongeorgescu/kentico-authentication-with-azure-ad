# Kentico Authentication with Azure Active Directory
Integrate Kentico with Azure Active Directory. <br/> 
This github repository is extracted initially from https://github.com/kate-orlova/azure-ad-auth-in-kentico

The following changes have been applied:

* [Missing instructions] Deploy CMS solution as virtual directory (no need to go "web application") <br/>
* Keep IIS_APPPOOL\DefaultAppPool (.NET 4.0 and managed integration pipe) <br/>
* [Missing instructions] Build it with x64 due to some very long package names <br/>
* [Missing instructions] Add an empty AzureADAuthentication.axd to the CMS project (root) <br/>
* [Missing instructions] On Azure Active Directory, assign to Kentico-CMS registered application the following API Permissions: <br/>
&nbsp;&nbsp;Azure Active Directory Graph: <br/>
&nbsp;&nbsp;&nbsp;&nbsp;Directory.ReadWrite.All <br/>
* [Improved business logic] In AuthenticateAzureDirectoryHandler add the creation of any Azure Active Directory with "editor" privilege by default; their access to CMS features will then be controlled through the role and its permissions <br/>
* [Improved business logic] Added validation for Azure Active Directory user whose groups do not match any Kentico roles. Raise exception and exit if no match found. No Kentico role-less user should be allowed to be created via Azure Active Directory authentication<br/>
* [Bug fix] Update the verb from "*" to "GET" in AzureADAuthentication.axd handler in web.config (called AzureADAuthenticationHandler, in the file with the same name) <br/> <br/>
* [Bug fix] Update the following line in Login.ascx.cs file: <br/>
btnAzureSignIn.NavigateUrl = authorizationUrl.AbsoluteUri.Replace("form_post","query"); <br/>
* [Bug fix] Update graph.windows.net to the newer graph.microsoft.com <br/>

* For testing purposes please use the following settings:

*** Azure Active Directory *********************************** <br/>
Authority URL: "https://login.microsoftonline.com/{0}" <br/>
Graph URL: "https://graph.microsoft.com/v1.0/{0}" <br/>
Return URI: http://localhost/MSApp/AzureADAuthentication.axd <br/>
Client ID: "5d6c194a-708b-4fd1-8e61-60bbdd47943e" <br/>
Tenant ID: "e8422127-880e-4288-928e-4ced14423628" <br/>
Application Key: "_\~DO93T.Udoq2gNw_2_n4JxBrG-18q~o9V" <br/> 
Redirect URIs: "http://localhost/CMSApp/AzureADAuthentication.axd" <br/>

Username: "azor@alviandalabs.onmicrosoft.com" <br/>
Group: "Dogs" <br/>
Password: "Alldogs2020!" <br/>

*** Kentico Admin: Settings *********************************** <br/>
Authentication Redirect Page: "/AzureADAuthentication.axd"





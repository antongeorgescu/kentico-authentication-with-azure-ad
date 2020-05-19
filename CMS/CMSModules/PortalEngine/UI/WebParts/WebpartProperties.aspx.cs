using CMS.UIControls;
using System;


public partial class CMSModules_PortalEngine_UI_WebParts_WebpartProperties : CMSWebPartPropertiesPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        RegisterModalPageScripts();

        rowsFrameset.Attributes["rows"] = TabsFrameHeight + ", *";
    }
}
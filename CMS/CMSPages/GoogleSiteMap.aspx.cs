using CMS.UIControls;
using System;

public partial class CMSPages_GoogleSiteMap : XMLPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/xml";
    }
}
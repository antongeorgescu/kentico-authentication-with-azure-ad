using CMS.DataEngine;
using CMS.UIControls;
using System;
using System.Web.UI;

public partial class CMSMessages_KickedUser : MessagePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        titleElem.TitleText = GetString("kicked.header");
        Page.Title = GetString("kicked.header");
        lblInfo.Text = String.Format(GetString("kicked.info"), SettingsKeyInfoProvider.GetIntValue("CMSDenyLoginInterval"));

        // Back link
        lnkBack.Text = GetString("general.Back");
        lnkBack.NavigateUrl = "~/";
    }
}
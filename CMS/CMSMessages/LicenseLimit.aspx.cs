using CMS.Helpers;
using CMS.UIControls;
using System;

public partial class CMSMessages_LicenseLimit : MessagePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var feature = QueryHelper.GetString("feature", "");

        titleElem.TitleText = GetString("LicenseLimitation.InfoPageTitle");
        lblMessage.Text = ResHelper.GetStringFormat("LicenseLimitation.InfoPageMessage", HTMLHelper.HTMLEncode(feature));
    }
}
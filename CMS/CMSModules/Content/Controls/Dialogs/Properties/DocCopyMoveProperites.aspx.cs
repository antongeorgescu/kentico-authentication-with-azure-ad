using CMS.Base.Web.UI;
using CMS.UIControls;
using System;


public partial class CMSModules_Content_Controls_Dialogs_Properties_DocCopyMoveProperites : CMSDeskPage
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ScriptHelper.RegisterWOpenerScript(this);
    }
}

using CMS.Helpers;
using CMS.UIControls;
using System;


[Title("EmailTemplate_Edit.Title")]
public partial class CMSModules_EmailTemplates_Pages_Frameset : CMSEmailTemplatesPage
{
    protected override void OnPreInit(EventArgs e)
    {
        base.OnPreInit(e);

        bool isDialog = QueryHelper.GetBoolean("editonlycode", false);
        if (isDialog)
        {
            frm.FrameHeight = TabsFrameHeight;
        }
    }
}
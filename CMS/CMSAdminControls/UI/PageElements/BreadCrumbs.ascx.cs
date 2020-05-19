using CMS.UIControls;
using System.Web.UI.WebControls;


public partial class CMSAdminControls_UI_PageElements_BreadCrumbs : Breadcrumbs
{
    #region "Properties"

    /// <summary>
    /// Help control.
    /// </summary>
    public HelpControl Help
    {
        get
        {
            return helpBreadcrumbs;
        }
    }


    /// <summary>
    /// Placeholder into which the breadcrumb items will be generated.
    /// </summary>
    protected override PlaceHolder BreadcrumbsContainer
    {
        get
        {
            return plcBreadcrumbs;
        }
    }

    #endregion
}
using CMS.Base;
using CMS.DocumentEngine.Web.UI;
using CMS.Helpers;
using System.Collections.Generic;
using System.Linq;


public partial class CMSModules_Newsletters_Controls_RecipientContactGroups : CMSAbstractTransformation
{
    protected const int MAX_DISPLAYED = 1;

    private List<string> mGroups;


    /// <summary>
    /// Listed contact groups
    /// </summary>
    public List<string> Groups
    {
        get
        {
            return mGroups ?? (mGroups = ((EnumerableDataContainer<string>)DataItem).Select(HTMLHelper.HTMLEncode).ToList());
        }
    }

}
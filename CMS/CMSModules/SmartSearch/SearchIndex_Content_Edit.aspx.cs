﻿using CMS.Base.Web.UI;
using CMS.CustomTables;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Search;
using CMS.UIControls;
using System;
using System.Web.UI;


public partial class CMSModules_SmartSearch_SearchIndex_Content_Edit : GlobalAdminPage
{
    private string itemType = QueryHelper.GetString("itemtype", SearchIndexSettingsInfo.TYPE_ALLOWED);
    private int indexId = QueryHelper.GetInteger("indexid", 0);
    private Guid itemGuid = QueryHelper.GetGuid("guid", Guid.Empty);
    private string indexType = TreeNode.OBJECT_TYPE;


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Get search index info object
        SearchIndexInfo sii = SearchIndexInfoProvider.GetSearchIndexInfo(indexId);
        if (sii != null)
        {
            // If doesn't exist select default type
            indexType = sii.IndexType;
        }

        ContentEdit.Visible = false;
        forumEdit.Visible = false;
        customTableEdit.Visible = false;
        onLineFormEdit.Visible = false;

        ContentEdit.StopProcessing = true;
        forumEdit.StopProcessing = true;
        customTableEdit.StopProcessing = true;
        onLineFormEdit.StopProcessing = true;

        // Set tabs according to index type
        switch (indexType)
        {
            case TreeNode.OBJECT_TYPE:
            case SearchHelper.DOCUMENTS_CRAWLER_INDEX:
                {
                    // Document index
                    ContentEdit.ItemID = indexId;
                    ContentEdit.ItemGUID = itemGuid;
                    ContentEdit.Visible = true;
                    ContentEdit.StopProcessing = false;
                }
                break;

            case PredefinedObjectType.FORUM:
                {
                    // Forum index type
                    forumEdit.ItemID = indexId;
                    forumEdit.ItemGUID = itemGuid;
                    forumEdit.Visible = true;
                    forumEdit.StopProcessing = false;
                }
                break;

            case CustomTableInfo.OBJECT_TYPE_CUSTOMTABLE:
                {
                    // Custom table index
                    customTableEdit.ItemID = indexId;
                    customTableEdit.ItemGUID = itemGuid;
                    customTableEdit.Visible = true;
                    customTableEdit.StopProcessing = false;
                }
                break;

            case PredefinedObjectType.BIZFORM:
                {
                    // Custom table index
                    onLineFormEdit.ItemID = indexId;
                    onLineFormEdit.ItemGUID = itemGuid;
                    onLineFormEdit.Visible = true;
                    onLineFormEdit.StopProcessing = false;
                }
                break;
        }

        ContentEdit.ItemType = itemType;
        forumEdit.ItemType = itemType;

        // Show messages
        if (!RequestHelper.IsPostBack())
        {
            if (QueryHelper.GetBoolean("saved", false))
            {
                ShowChangesSaved();
            }
            if (QueryHelper.GetBoolean("rebuild", false))
            {
                ShowRebuildMessage();
            }
        }
    }


    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        // Set item type
        switch (indexType)
        {
            case TreeNode.OBJECT_TYPE:
                itemType = ContentEdit.ItemType;
                break;
            case PredefinedObjectType.FORUM:
                itemType = forumEdit.ItemType;
                break;
        }

        // Create breadcrumbs
        CreateBreadcrums();
    }


    /// <summary>
    /// Creates breadcrumbs
    /// </summary>
    private void CreateBreadcrums()
    {
        PageBreadcrumbs.AddBreadcrumb(new BreadcrumbItem
        {
            Text = GetString("srch.index.itemlist"),
            RedirectUrl = "~/CMSModules/SmartSearch/SearchIndex_Content_List.aspx?indexid=" + indexId,
        });

        if (itemGuid != Guid.Empty)
        {
            PageBreadcrumbs.AddBreadcrumb(new BreadcrumbItem
            {
                Text = GetString("srch.index.currentitem"),
            });
        }
        else
        {
            // Allowed or Excluded item
            PageBreadcrumbs.AddBreadcrumb(new BreadcrumbItem
            {
                Text = itemType == SearchIndexSettingsInfo.TYPE_ALLOWED ? GetString("srch.index.newtitemallowed") : GetString("srch.index.newtitemexcluded"),
            });
        }

        // Do not provide suffix
        UIHelper.SetBreadcrumbsSuffix("");
    }


    private void ShowRebuildMessage()
    {
        Control control;
        switch (indexType)
        {
            case PredefinedObjectType.FORUM:
                control = forumEdit;
                break;

            case CustomTableInfo.OBJECT_TYPE_CUSTOMTABLE:
                control = customTableEdit;
                break;

            case PredefinedObjectType.BIZFORM:
                control = onLineFormEdit;
                break;

            default:
                control = ContentEdit;
                break;
        }
        ShowInformation(String.Format(GetString("srch.indexrequiresrebuild"), "<a href=\"javascript:" + Page.ClientScript.GetPostBackEventReference(control, "saved") + "\">" + GetString("General.clickhere") + "</a>"));
    }
}

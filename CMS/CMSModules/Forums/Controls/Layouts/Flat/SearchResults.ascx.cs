﻿using CMS.Forums;
using CMS.Forums.Web.UI;
using CMS.Helpers;
using System;
using System.Data;


public partial class CMSModules_Forums_Controls_Layouts_Flat_SearchResults : ForumViewer
{
    private string pathSeparator = "&nbsp;&gt;&nbsp;";


    protected void Page_Load(object sender, EventArgs e)
    {
        DataSet results = GetSearchDataSet();

        // Bind the results to the control displaying it
        if (!DataHelper.DataSourceIsEmpty(results))
        {
            uniForumPosts.DataSource = results;
            uniForumPosts.DataBind();


            #region "Results info"

            string path = String.Empty;
            string[] splited = null;

            // If search in threads is enabled
            if (SearchInThreads != String.Empty)
            {
                splited = SearchInThreads.Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if ((splited != null) && (splited.Length == 1))
                {
                    // Get post info
                    ForumPostInfo fpi = ForumPostInfoProvider.GetForumPostInfo(ValidationHelper.GetInteger(splited[0], 0));
                    if (fpi != null)
                    {
                        // Get path from Post subject
                        path = HTMLHelper.HTMLEncode(fpi.PostSubject);

                        // Get forum info
                        ForumInfo fi = ForumInfoProvider.GetForumInfo(fpi.PostForumID);
                        if (fi != null)
                        {
                            // Get path from forum display name and post subject
                            path = HTMLHelper.HTMLEncode(fi.ForumDisplayName) + pathSeparator + path;

                            // Get forum group info
                            ForumGroupInfo fgi = ForumGroupInfoProvider.GetForumGroupInfo(fi.ForumGroupID);
                            if (fgi != null)
                            {
                                // Get path from group name, display name and post subject
                                path = HTMLHelper.HTMLEncode(fgi.GroupDisplayName) + pathSeparator + path;
                            }
                        }

                        path = GetString("ForumSearch.InThread") + path;
                    }
                }
            }
            else if (SearchInForums != String.Empty)
            {
                splited = SearchInForums.Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if ((splited != null) && (splited.Length == 1))
                {
                    // Get forum info
                    ForumInfo fi = ForumInfoProvider.GetForumInfo(ValidationHelper.GetInteger(splited[0], 0));
                    if (fi != null)
                    {
                        // Path is forum display name
                        path = HTMLHelper.HTMLEncode(fi.ForumDisplayName);

                        // Get forum group info
                        ForumGroupInfo fgi = ForumGroupInfoProvider.GetForumGroupInfo(fi.ForumGroupID);
                        if (fgi != null)
                        {
                            // Path is group display name and forum display name
                            path = HTMLHelper.HTMLEncode(fgi.GroupDisplayName) + pathSeparator + path;
                        }

                        path = GetString("ForumSearch.InForum") + path;
                    }
                }
            }
            else if (SearchInGroups != null)
            {
                splited = SearchInGroups.Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                if ((splited != null) && (splited.Length == 1))
                {
                    // Get forum group info
                    ForumGroupInfo fgi = ForumGroupInfoProvider.GetForumGroupInfo(splited[0], SiteID, CommunityGroupID);
                    if (fgi != null)
                    {
                        // Path is group display name
                        path = HTMLHelper.HTMLEncode(fgi.GroupDisplayName);

                        path = GetString("ForumSearch.InForumGroup") + path;
                    }
                }
            }

            // Format search result
            ltlResultsInfo.Text = "<div class=\"ForumSearchResultsInfo\">" + String.Format(GetString("ForumSearch.SearchResultsInfo"), HTMLHelper.HTMLEncode(QueryHelper.GetString("searchtext", "")), path, results.Tables[0].Rows.Count) + "</div>";

            #endregion
        }
        else
        {
            // No search results
            lblNoResults.Visible = true;
            plcResults.Visible = false;
            lblNoResults.Text = SearchNoResults;
        }
    }


    /// <summary>
    /// Encode text
    /// </summary>
    /// <param name="value">Input value</param>
    public string Encode(object value)
    {
        return HTMLHelper.HTMLEncode(ValidationHelper.GetString(value, ""));
    }
}
﻿using CMS.Helpers;
using CMS.Synchronization;
using CMS.UIControls;
using System;

public partial class CMSPages_IntegrationNotify : CMSPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string connectorName = QueryHelper.GetString("connectorName", string.Empty);
        if (string.IsNullOrEmpty(connectorName))
        {
            // Process external tasks for all connectors
            IntegrationHelper.ProcessExternalTasksAsync();
        }
        else
        {
            // Process external tasks of specified connector
            IntegrationHelper.ProcessExternalTasksAsync(connectorName);
        }
    }
}
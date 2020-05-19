using CMS.UIControls;
using System;

public partial class CMSInstall_Controls_WizardSteps_WagDialog : CMSUserControl
{
    #region "Methods"

    protected void Page_Load(object sender, EventArgs e)
    {
    }


    /// <summary>
    /// Creates requested license keys. Returns false if something fail.
    /// </summary>
    /// <param name="connectionString">Connection string</param>
    public bool ProcessRegistration(string connectionString)
    {
        return (connectionString == null);
    }

    #endregion
}
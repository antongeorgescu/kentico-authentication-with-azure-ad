using AzureADAuthentication;
using CMS;
using CMS.Base;
using CMS.DataEngine;
using CMS.EventLog;
using CMS.FormEngine;
using CMS.FormEngine.Web.UI;
using System;

[assembly: RegisterModule(typeof(AzureADAuthenticationModule))]
namespace AzureADAuthentication
{
    public class AzureADAuthenticationModule : Module
    {
        public AzureADAuthenticationModule()
            : base("AzureADAuthentication")
        {
        }

        protected override void OnInit()
        {
            EnsureCmsUserAzureCustomField();
        }

        private void EnsureCmsUserAzureCustomField()
        {
            var cmsUserDataClass = DataClassInfoProvider.GetDataClassInfo("cms.user");
            if (cmsUserDataClass == null)
            {
                return;
            }

            var formInfo = new FormInfo(cmsUserDataClass.ClassFormDefinition);
            if (formInfo.FieldExists("AzureADUsername"))
            {
                EventLogProvider.LogInformation("AzureADAuthentication", "Skip Create Field", "AzureADUsername");
                return;
            }

            // Create "AzureADUsername" field if it doesn't exist
            EventLogProvider.LogInformation("AzureADAuthentication", "Create Field", "AzureADUsername");

            var azureAdUsernameTextField = new FormFieldInfo
            {
                Name = "AzureADUsername",
                DataType = "text",
                Size = 200,
                Precision = -1,
                AllowEmpty = true,
                DefaultValue = string.Empty,
                System = false,
                FieldType = FormFieldControlTypeEnum.TextBoxControl,
                Visible = true,
                Caption = "Azure AD Username",
                Enabled = true
            };

            using (var tr = new CMSLateBoundTransaction())
            {
                var tm = new TableManager(cmsUserDataClass.ClassConnectionString);
                tr.BeginTransaction();

                var newFieldHandler = (AbstractAdvancedHandler)null;
                try
                {
                    newFieldHandler =
                        DataDefinitionItemEvents.AddItem.StartEvent(cmsUserDataClass, azureAdUsernameTextField);

                    var sqlType = DataTypeManager.GetSqlType(azureAdUsernameTextField.DataType,
                        azureAdUsernameTextField.Size, azureAdUsernameTextField.Precision);
                    tm.AddTableColumn(cmsUserDataClass.ClassTableName, azureAdUsernameTextField.Name, sqlType,
                        azureAdUsernameTextField.AllowEmpty, azureAdUsernameTextField.DefaultValue);

                    formInfo.AddFormItem(azureAdUsernameTextField);

                    cmsUserDataClass.ClassFormDefinition = formInfo.GetXmlDefinition();
                    cmsUserDataClass.ClassXmlSchema = tm.GetXmlSchema(cmsUserDataClass.ClassTableName);
                    DataClassInfoProvider.SetDataClassInfo(cmsUserDataClass);
                    FormHelper.UpdateInheritedClasses(cmsUserDataClass);

                    QueryInfoProvider.ClearDefaultQueries(cmsUserDataClass, true, true);
                    newFieldHandler.FinishEvent();

                    tr.Commit();

                    ClearHashtables("cms.user");
                }
                catch (Exception ex)
                {
                    EventLogProvider.LogException("AzureADAuthentication", "Create Field", ex);
                }
                finally
                {
                    newFieldHandler?.Dispose();
                }
            }
        }

        private void ClearHashtables(string className)
        {
            ClassStructureInfo.Remove(className, true);
            FormEngineWebUIResolvers.ClearResolvers(true);

            var ti = ObjectTypeManager.GetTypeInfo(className);
            if (ti == null || ti.ProviderType == null) return;

            ti.InvalidateColumnNames();
            ti.InvalidateAllObjects();
        }
    }
}
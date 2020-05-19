using CMS;
using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine.Web.UI;
using CMS.TranslationServices;

[assembly: RegisterModule(typeof(TranslationServicesMethodsModule))]

/// <summary>
/// Registers translation services methods.
/// </summary>
public class TranslationServicesMethodsModule : Module
{
    public TranslationServicesMethodsModule()
        : base("CMS.TranslationServicesMethods")
    {
    }


    protected override void OnInit()
    {
        Extend<TransformationNamespace>.With<TranslationServicesMethods>();
    }
}

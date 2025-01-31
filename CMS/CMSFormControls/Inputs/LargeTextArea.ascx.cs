﻿using CMS.Base;
using CMS.Base.Web.UI;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;
using CMS.SiteProvider;
using CMS.TranslationServices;
using System;
using System.Collections;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;


/// <summary>
/// Text area form control with syntax highlighting support.
/// </summary>
public partial class CMSFormControls_Inputs_LargeTextArea : FormEngineUserControl, ICallbackEventHandler
{
    #region "Variables"

    private bool mAllowMacros = true;
    private string mIdentifier;
    private string mCallbackResult = null;

    #endregion


    #region "Properties"

    /// <summary>
    /// Gets or sets whether macros are allowed.
    /// </summary>
    /// <value>True, if macros can be inserted, otherwise false</value>
    [Browsable(true)]
    [Description("Determines whether macros are allowed")]
    [Category("Form Control")]
    [DefaultValue(true)]
    public bool AllowMacros
    {
        get
        {
            return (Form != null ? Form.AllowMacroEditing : mAllowMacros);
        }
        set
        {
            mAllowMacros = value;
        }
    }


    /// <summary>
    /// Gets the ExtendedTextArea object used in this form control.
    /// </summary>
    [Browsable(false)]
    public ExtendedTextArea TextArea
    {
        get
        {
            cntElem.LoadContainer();
            return txtArea;
        }
    }


    /// <summary>
    /// Gets or sets whether this form control is enabled.
    /// </summary>
    /// <value>True, if form control is enabled, otherwise false</value>
    [Browsable(true)]
    [Description("Determines whether this form control is enabled")]
    [Category("Form Control")]
    [DefaultValue(true)]
    public override bool Enabled
    {
        get
        {
            return base.Enabled;
        }
        set
        {
            base.Enabled = TextArea.Enabled = btnMore.Enabled = value;
        }
    }


    /// <summary>
    /// Gets the server control identifier generated by ASP.NET.
    /// </summary>
    [Browsable(false)]
    public override string InputClientID
    {
        get
        {
            return TextArea.ClientID;
        }
    }


    /// <summary>
    /// Gets or sets the value of this form control.
    /// </summary>
    /// <value>Text content of this editor</value>
    [Browsable(false)]
    public override object Value
    {
        get
        {
            return TextArea.Text;
        }
        set
        {
            TextArea.Text = (string)value;
        }
    }


    /// <summary>
    /// Gets the client ID of the embedded element that provides the value for this control.
    /// </summary>
    [Browsable(false)]
    public override string ValueElementID
    {
        get
        {
            return TextArea.ClientID;
        }
    }


    /// <summary>
    /// Gets or sets the width of this control.
    /// </summary>
    /// <value>The width in pixels or percentage</value>
    [Browsable(true)]
    [Description("Determines the width of this control")]
    [Category("Appearance")]
    [DefaultValue("250px")]
    public Unit Width
    {
        get
        {
            return TextArea.Width;
        }
        set
        {
            TextArea.Width = value;
        }
    }


    /// <summary>
    /// Gets or sets the height of this control.
    /// </summary>
    /// <value>The height in pixels or percentage</value>
    [Browsable(true)]
    [Description("Determines the width of this control")]
    [Category("Appearance")]
    [DefaultValue("50px")]
    public Unit Height
    {
        get
        {
            return TextArea.Height;
        }
        set
        {
            TextArea.Height = value;
        }
    }


    /// <summary>
    /// Dialog control identifier.
    /// </summary>
    private string Identifier
    {
        get
        {
            if (string.IsNullOrEmpty(mIdentifier))
            {
                mIdentifier = Guid.NewGuid().ToString();
            }
            return mIdentifier;
        }
    }


    /// <summary>
    /// Indicates if Tab key adds '\t' into the text. Default value is true.
    /// </summary>
    [Browsable(false)]
    public bool EnableTabKey
    {
        get
        {
            return TextArea.EnableTabKey;
        }
        set
        {
            TextArea.EnableTabKey = value;
        }
    }

    #endregion


    #region "Translation properties"

    /// <summary>
    /// Gets or sets translation element client ID. Text from this id will be translated.
    /// </summary>
    public string TranslationElementClientID
    {
        get;
        set;
    }


    /// <summary>
    /// Gets or sets text to translate. This text will be translated if nothing is in text area.
    /// </summary>
    public string TranslationSourceText
    {
        get;
        set;
    }


    /// <summary>
    /// If translation services are allowed. Source language of translation.
    /// </summary>
    public string TranslationSourceLanguage
    {
        get;
        set;
    }


    /// <summary>
    /// If translation services are allowed. Source language of translation.
    /// </summary>
    public string TranslationTargetLanguage
    {
        get;
        set;
    }


    /// <summary>
    /// Indicates if machine translation services are supported for this text area.
    /// </summary>
    public bool AllowTranslationServices
    {
        get;
        set;
    }

    #endregion


    #region "Methods"

    /// <summary>
    /// Handles the Load event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The System.EventArgs instance containing the event data</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        CheckMinMaxLength = true;
        CheckRegularExpression = true;

        btnMore.ToolTip = GetString("general.editinnew");

        ScriptHelper.RegisterJQuery(Page);

        // Register general dialog scripts
        ScriptHelper.RegisterDialogScript(Page);

        // Assign handler that opens modal dialog
        btnMore.OnClientClick += Page.ClientScript.GetCallbackEventReference(this, null, "ShowLargeTextAreaDesigner", null) + "; return false;";

        // Register modal dialog script
        ScriptHelper.RegisterClientScriptBlock(this, typeof(string), "ShowLargeTextAreaDesigner", GetDesignerScript());

        if (AllowTranslationServices)
        {
            RegisterTranslationScripts();

            // Translation services processing
            RegisterTranslationServicesActions();
        }
    }


    /// <summary>
    /// Builds and returns a client script that is used to open modal dialog with an editor in advanced mode.
    /// </summary>
    /// <returns>String that represents a modal dialog JS script.</returns>
    private string GetDesignerScript()
    {
        string script = string.Format(
            @"function ShowLargeTextAreaDesigner(queryString) 
              {{
                modalDialog('{0}' + queryString, 'ShowLargeTextAreaDesigner', 1034, {1}); return false;
              }}",
            ResolveUrl("~/CMSFormControls/Selectors/LargeTextAreaDesigner.aspx"),
            (680 - (AllowMacros ? 0 : 60))
            );

        return ScriptHelper.GetScript(script);
    }


    /// <summary>
    /// Returns value of specified property
    /// </summary>
    /// <param name="propertyName">Name of the property</param>
    public override object GetValue(string propertyName)
    {
        switch (propertyName.ToLowerCSafe())
        {
            case "translationsourcetext":
                return TranslationSourceText;

            case "translationsourcelanguage":
                return TranslationSourceLanguage;

            case "translationtargetlanguage":
                return TranslationTargetLanguage;

            case "allowtranslationservices":
                return AllowTranslationServices;

            case "processmacrosecurity":
                return TextArea.ProcessMacroSecurity;

            case "width":
                return Width;

            case "translationelementclientid":
                return TranslationElementClientID;

            case "enabletabkey":
                return TextArea.EnableTabKey;
        }

        return null;
    }


    /// <summary>
    /// Sets value of specified property.
    /// </summary>
    /// <param name="propertyName">Name of the property</param>
    /// <param name="value">Value to set</param>
    public override bool SetValue(string propertyName, object value)
    {
        switch (propertyName.ToLowerCSafe())
        {
            case "translationsourcetext":
                TranslationSourceText = ValidationHelper.GetString(value, null);
                break;

            case "translationsourcelanguage":
                TranslationSourceLanguage = ValidationHelper.GetString(value, null);
                break;

            case "translationtargetlanguage":
                TranslationTargetLanguage = ValidationHelper.GetString(value, null);
                break;

            case "allowtranslationservices":
                AllowTranslationServices = ValidationHelper.GetBoolean(value, false);
                break;

            case "processmacrosecurity":
                TextArea.ProcessMacroSecurity = ValidationHelper.GetBoolean(value, true);
                break;

            case "width":
                string width = ValidationHelper.GetString(value, String.Empty);
                Width = new Unit(width);
                break;

            case "translationelementclientid":
                TranslationElementClientID = ValidationHelper.GetString(value, null);
                break;

            case "enabletabkey":
                TextArea.EnableTabKey = ValidationHelper.GetBoolean(value, true);
                break;
        }

        return true;
    }


    /// <summary>
    /// Registers scripts needed for translation services
    /// </summary>
    public void RegisterTranslationScripts()
    {
        ScriptHelper.RegisterClientScriptBlock(this.Page, typeof(string), "CMSTextBoxTranslationScript", @"
function SetValueToTextBox(text, arg) { 
  var args = arg.split(';');
  if (text != '') { 
    var txtBox = document.getElementById(args[0]);
    if (txtBox != null) {
      txtBox.value = text; 
    }
  } else {
    alert(" + ScriptHelper.GetLocalizedString("translation.createdefaulttranslation") + @");
  }

  var icon = document.getElementById(args[1]);
  if (icon != null) {
    icon.className = args[2]; 
  }
}

function StartProgress(iconClientId) { 
  var icon = document.getElementById(iconClientId);
  if (icon != null) {
    icon.className = 'icon-spinner spinning'; 
  }
}", true);
    }


    /// <summary>
    /// Returns rendered panel with translation services
    /// </summary>
    public void RegisterTranslationServicesActions()
    {
        string currentSiteName = SiteContext.CurrentSiteName;
        if (TranslationServiceHelper.IsTranslationAllowed(currentSiteName))
        {
            string where = "TranslationServiceEnabled = 1 AND TranslationServiceIsMachine = 1";

            foreach (TranslationServiceInfo tsi in TranslationServiceInfoProvider.GetTranslationServices(where, "TranslationServiceDisplayName", 0, "TranslationServiceDisplayName, TranslationServiceName"))
            {
                AddTranslationControl(tsi, currentSiteName);
            }
        }
    }


    /// <summary>
    /// Add translation control to the list of action buttons
    /// </summary>
    /// <param name="tsi">Translation service object used to initialize the control</param>
    /// <param name="siteName">Site name to check for service availability</param>
    private void AddTranslationControl(TranslationServiceInfo tsi, string siteName)
    {
        string arg = "'##SERVICE##|' + document.getElementById('" + (TranslationElementClientID ?? InputClientID) + @"').value";
        if (TranslationServiceHelper.IsServiceAvailable(tsi.TranslationServiceName, siteName))
        {
            var ctrl = new CMSAccessibleButton();
            cntElem.ActionsContainer.Controls.Add(ctrl);

            ctrl.IconCssClass = "icon-" + tsi.TranslationServiceName.ToLowerCSafe();
            ctrl.ToolTip = string.Format("{0} {1}", ResHelper.GetString("translations.translateusing"), tsi.TranslationServiceDisplayName);

            // Get callback reference for translation
            string cbRef = Page.ClientScript.GetCallbackEventReference(this, arg.Replace("##SERVICE##", tsi.TranslationServiceName), "SetValueToTextBox", "'" + InputClientID + ";" + ctrl.ClientID + "_icon;" + ctrl.IconCssClass + "'", true);
            ctrl.OnClientClick = "StartProgress('" + ctrl.ClientID + "_icon'); " + cbRef + ";return false;";
        }
    }

    #endregion


    #region "ICallbackEventHandler Members"

    /// <summary>
    /// Raise callback method.
    /// </summary>
    /// <param name="eventArgument">Service name</param>
    public void RaiseCallbackEvent(string eventArgument)
    {
        mCallbackResult = null;
        string[] args = eventArgument.Split('|');

        // If 2 parameters, it's translation request
        if (args.Length == 2)
        {
            mCallbackResult = GetTranslatedText(args[0], args[1]);
        }
        else
        {
            mCallbackResult = GetEditorDialogParameters();
        }
    }


    /// <summary>
    /// Pass the parameters to newly opened window and returns reference to these dialog parameters.
    /// </summary>
    /// <returns>URL containing reference to dialog parameters</returns>
    private string GetEditorDialogParameters()
    {
        Hashtable parameters = new Hashtable();

        parameters["editorid"] = ValueElementID;
        parameters["allowmacros"] = AllowMacros.ToString().ToLowerCSafe();

        if (!string.IsNullOrEmpty(ResolverName))
        {
            parameters["resolvername"] = ResolverName;
        }
        WindowHelper.Add(Identifier, parameters);

        string queryString = "?params=" + Identifier;
        return URLHelper.AddParameterToUrl(queryString, "hash", QueryHelper.GetHash(queryString));
    }


    /// <summary>
    /// Get translation of an original text.
    /// </summary>
    /// <param name="serviceName">Name of translation service to use</param>
    /// <param name="textToTranslate">Text to be translated</param>
    /// <returns>Translation of an original text</returns>
    private string GetTranslatedText(string serviceName, string textToTranslate)
    {
        TranslationServiceInfo info = TranslationServiceInfoProvider.GetTranslationServiceInfo(serviceName);
        if (info != null)
        {
            if (info.TranslationServiceIsMachine)
            {
                AbstractMachineTranslationService service = AbstractMachineTranslationService.GetTranslationService(info, SiteContext.CurrentSiteName);
                if (service != null)
                {
                    if (string.IsNullOrEmpty(textToTranslate))
                    {
                        textToTranslate = TranslationSourceText ?? String.Empty;
                    }

                    string sourceLang = TranslationSourceLanguage ?? CultureHelper.DefaultUICultureCode;
                    string targetLang = TranslationTargetLanguage ?? CultureHelper.PreferredUICultureCode;

                    string translation = service.Translate(textToTranslate, sourceLang, targetLang) ?? String.Empty;

                    // Translators may add a dot to the translation - remove it if it was not a part of the original text
                    if (!textToTranslate.EndsWithCSafe("."))
                    {
                        translation = translation.TrimEnd('.');
                    }

                    return translation;
                }
            }
        }

        return null;
    }


    /// <summary>
    /// Gets callback result.
    /// </summary>
    public string GetCallbackResult()
    {
        return mCallbackResult;
    }

    #endregion
}
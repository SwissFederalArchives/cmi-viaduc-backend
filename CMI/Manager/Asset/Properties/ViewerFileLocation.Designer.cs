﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CMI.Manager.Asset.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.2.0.0")]
    internal sealed partial class ViewerFileLocation : global::System.Configuration.ApplicationSettingsBase {
        
        private static ViewerFileLocation defaultInstance = ((ViewerFileLocation)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new ViewerFileLocation())));
        
        public static ViewerFileLocation Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.ViewerFileLocation.ManifestOutputSaveDirectory@@")]
        public string ManifestOutputSaveDirectory {
            get {
                return ((string)(this["ManifestOutputSaveDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.ViewerFileLocation.ContentOutputSaveDirectory@@")]
        public string ContentOutputSaveDirectory {
            get {
                return ((string)(this["ContentOutputSaveDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.ViewerFileLocation.OcrOutputSaveDirectory@@")]
        public string OcrOutputSaveDirectory {
            get {
                return ((string)(this["OcrOutputSaveDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.ViewerFileLocation.ImageOutputSaveDirectory@@")]
        public string ImageOutputSaveDirectory {
            get {
                return ((string)(this["ImageOutputSaveDirectory"]));
            }
        }
    }
}

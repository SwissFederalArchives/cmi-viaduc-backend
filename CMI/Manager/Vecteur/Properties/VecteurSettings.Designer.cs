﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CMI.Manager.Vecteur.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class VecteurSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static VecteurSettings defaultInstance = ((VecteurSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new VecteurSettings())));
        
        public static VecteurSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\vecteur")]
        public string BaseDirectory {
            get {
                return ((string)(this["BaseDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.Address@@")]
        public string Address {
            get {
                return ((string)(this["Address"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("11127")]
        public long SftpPort {
            get {
                return ((long)(this["SftpPort"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.ApiKey@@")]
        public string ApiKey {
            get {
                return ((string)(this["ApiKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int RequestTimeoutInMinute {
            get {
                return ((int)(this["RequestTimeoutInMinute"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPassword@@")]
        public string SftpPassword {
            get {
                return ((string)(this["SftpPassword"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpLicenseKey@@")]
        public string SftpLicenseKey {
            get {
                return ((string)(this["SftpLicenseKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPrivateCertKey@@")]
        public string SftpPrivateCertKey {
            get {
                return ((string)(this["SftpPrivateCertKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPrivateCertPassword@@")]
        public string SftpPrivateCertPassword {
            get {
                return ((string)(this["SftpPrivateCertPassword"]));
            }
        }
    }
}

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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.9.0.0")]
    internal sealed partial class StorageProvider : global::System.Configuration.ApplicationSettingsBase {
        
        private static StorageProvider defaultInstance = ((StorageProvider)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new StorageProvider())));
        
        public static StorageProvider Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.StorageProvider.AccessKey@@")]
        public string AccessKey {
            get {
                return ((string)(this["AccessKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.StorageProvider.SecretAccessKey@@")]
        public string SecretAccessKey {
            get {
                return ((string)(this["SecretAccessKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.StorageProvider.BucketName@@")]
        public string BucketName {
            get {
                return ((string)(this["BucketName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.StorageProvider.Region@@")]
        public string Region {
            get {
                return ((string)(this["Region"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.Asset.Properties.StorageProvider.ServiceUrl@@")]
        public string ServiceUrl {
            get {
                return ((string)(this["ServiceUrl"]));
            }
        }
    }
}

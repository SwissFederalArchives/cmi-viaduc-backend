using System;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using Serilog;

namespace CMI.Web.Frontend.api.Configuration
{
    public static class ServerSettings
    {
        public static T GetServerSettings<T>(string entryPath) where T : SettingsEntry
        {
            T entry = default;
            try
            {
                entry = SettingsHelper.GetSettingsFor<T>(FrontendSettingsViaduc.Instance.GetServerSettings(), entryPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not read Setting with entryPath:{entryPath}", entryPath);
            }

            return entry;
        }

        public static T GetServerSettings<T>() where T : SettingsEntry
        {
            var relativePath = Reflective.GetValue<string>(typeof(T), "RelativePath");
            return GetServerSettings<T>(relativePath);
        }
    }
}
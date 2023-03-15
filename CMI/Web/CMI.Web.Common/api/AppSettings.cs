using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Common.api
{
    public class AppSettings : ITranslator
    {
        private readonly Dictionary<string, JObject> translations = new Dictionary<string, JObject>();
        private JObject serverSettings;
        private JObject settings;

        public AppSettings(Assembly assembly)
        {
            ConfigDirectory = DirectoryHelper.Instance.ConfigDirectory;
            ClientConfigDirectory = DirectoryHelper.Instance.ClientConfigDirectory;

            ServiceAssembly = assembly ?? Assembly.GetExecutingAssembly();
        }

        public string ConfigDirectory { get; protected set; }
        public string ClientConfigDirectory { get; protected set; }

        private Assembly ServiceAssembly { get; }

        public string GetTranslation(string language, string path, string defaultText = null)
        {
            string text = null;
            var texts = GetTranslations(language);
            if (texts != null)
            {
                var token = JsonHelper.GetByPath(texts, path);
                if (token != null)
                {
                    text = token.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                text = language != "de"
                    ? $"(!{language}){defaultText}"
                    : defaultText;
            }

            return text;
        }

        public JObject GetTranslations(string language)
        {
            if (!translations.ContainsKey(language))
            {
                InitTranslationsFor(language);
            }

            return translations[language];
        }

        public JObject GetSettings()
        {
            lock (this)
            {
                if (settings == null)
                {
                    try
                    {
                        settings = BuildSettingsData(ConfigDirectory, ClientConfigDirectory);
                        DecorateAndCleanupSettingsData(settings);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not init settings");
                    }
                }

                InjectSettingsForRequest(settings);
                return settings;
            }
        }

        public JObject GetServerSettings()
        {
            lock (this)
            {
                if (serverSettings == null)
                {
                    try
                    {
                        serverSettings = BuildSettingsData(ConfigDirectory, ClientConfigDirectory, true);
                        DecorateAndCleanupSettingsData(serverSettings);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not init server settings");
                    }
                }

                InjectSettingsForRequest(serverSettings);
                return serverSettings;
            }
        }

        /// <summary>
        ///     Settings, die bei jedem Request neu ausgelesen werden müssen
        /// </summary>
        /// <param name="settingsObj"></param>
        private void InjectSettingsForRequest(JObject settingsObj)
        {
            if (SettingsHelper.FindAttributeValue<JToken>(settingsObj, "chatbot") == null)
            {
                SettingsHelper.InjectInfo(settingsObj, "chatbot", "supportedLanguagesForChatBot", WebHelper.SupportedLanguagesForChatBot);
                SettingsHelper.InjectInfo(settingsObj, "chatbot", "urlForChatBot", WebHelper.UrlForChatBot);
                SettingsHelper.InjectInfo(settingsObj, "matomo", "url", WebHelper.MatomoUrl);
                SettingsHelper.InjectInfo(settingsObj, "matomo", "siteId", WebHelper.MatomoSiteId);
                SettingsHelper.InjectInfo(settingsObj, "viewer", "url", WebHelper.ViewerUrl);
            }
        }

        #region Protected methods

        private void InitTranslationsFor(string language)
        {
            try
            {
                translations[language] = new JObject();
                var path = StringHelper.AddToString(ClientConfigDirectory, @"\", $"translations.{language.ToLower()}.json");

                if (!File.Exists(path))
                {
                    return;
                }

                var trans = JsonHelper.GetJsonFromFile(path);
                if (trans != null)
                {
                    translations[language] = trans;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not init translations for {language}", language);
            }
        }

        protected JObject BuildSettingsData(string configDirectory, string clientConfigDirectory, bool allowInternal = false)
        {
            var newSettings = new JObject();
            var path = StringHelper.AddToString(clientConfigDirectory, @"\", "settings.json");

            if (File.Exists(path))
            {
                var clientSettings = JsonHelper.GetJsonFromFile(path);
                if (clientSettings != null)
                {
                    SettingsHelper.UpdateSettingsWith(newSettings, clientSettings, allowInternal);
                }
            }

            var configuration = JsonHelper.FindTokenValue<JObject>(newSettings, "configuration");
            if (configuration == null)
            {
                configuration = new JObject();
                newSettings.Add("configuration", configuration);
            }

            path = StringHelper.AddToString(configDirectory, @"\", "config.json");
            if (File.Exists(path))
            {
                var customConfiguration = JsonHelper.GetJsonFromFile(path);
                if (customConfiguration != null)
                {
                    SettingsHelper.UpdateSettingsWith(configuration, customConfiguration, allowInternal);
                }
            }

            return newSettings;
        }

        /// <summary>
        ///     Diese Settings werden beim APPLIKATIONSSTART einmalig in die Config Injected.
        ///     Die Configs wird gecached, daher müssen Settings, die bei jedem Request neu ausgelesen werden, anderorts eingebaut
        ///     werden
        /// </summary>
        private void DecorateAndCleanupSettingsData(JObject settingsData)
        {
            SettingsHelper.InjectServiceAssemblyInfo(settingsData, ServiceAssembly);
            SettingsHelper.CleanupSettings(settingsData);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CMI.Access.Harvest.Properties;
using Serilog;

namespace CMI.Access.Harvest
{
    public class LanguageSettings
    {
        public LanguageSettings()
        {
            try
            {
                DefaultLanguage = new CultureInfo(Settings.Default.DefaultLanguage);
                var languages = Settings.Default.SupportedLanguages.Split(',').Select(t => t.Trim());
                SupportedLanguages = languages.Select(l => new CultureInfo(l)).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid language configuration in configuration file. Check the used language codes");
                DefaultLanguage = new CultureInfo("de-CH");
                SupportedLanguages = new List<CultureInfo>();
            }
        }

        public CultureInfo DefaultLanguage { get; set; }
        public List<CultureInfo> SupportedLanguages { get; set; }
    }
}
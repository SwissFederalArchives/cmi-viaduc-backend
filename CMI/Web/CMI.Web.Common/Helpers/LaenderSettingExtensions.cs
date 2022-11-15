using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Common.Helpers
{
    public static class LaenderSettingExtensions
    {
        public static JArray GetCountries(this ILaenderSetting laenderSetting, string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            language = language.ToLowerInvariant();
            if (!WebHelper.SupportedLanguages.Contains(language))
            {
                language = WebHelper.DefaultLanguage;
            }

            var countries = new JArray();
            var allCountries = (JObject) JsonConvert.DeserializeObject(laenderSetting.Laender);

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (JProperty prop in allCountries.Children())
            {
                var translated = JsonHelper.FindTokenValue<string>(prop.Value, language);
                var canOnboardWithPassport = JsonHelper.FindTokenValue<bool>(prop.Value, "canOnboardWithPassport");
                var canOnboardWithIdentityCard = JsonHelper.FindTokenValue<bool>(prop.Value, "canOnboardWithIdentityCard");
                var newLaenderCode = JsonHelper.FindTokenValue<string>(prop.Value, "Laendercode");
                if (string.IsNullOrEmpty(translated))
                {
                    translated = prop.Name;
                }

                countries.Add(new JObject
                {
                    {"code", prop.Name},
                    {"name", translated},
                    {"canOnboardWithPassport", canOnboardWithPassport},
                    {"canOnboardWithIdentityCard", canOnboardWithIdentityCard},
                    {"newLaenderCode", newLaenderCode}
                });
            }

            return countries;
        }
    }


    public interface ILaenderSetting
    {
        string Laender { get; set; }
    }
}
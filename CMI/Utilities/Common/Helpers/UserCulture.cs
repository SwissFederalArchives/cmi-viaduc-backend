using System;
using System.Globalization;
using System.Threading;

namespace CMI.Utilities.Common.Helpers
{
    public class UserCulture : IUserCulture
    {
        /// <summary>
        ///     Runs an Action with the cultureinfo of the user set to ensure dates and numbers are in users language
        ///     Only needed for service-actions, as the web sets users cultureinfo's on request base
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="language">users language (ex: "de")</param>
        /// <param name="func">the action that should be run between the cultureinfo set</param>
        /// <returns></returns>
        public T RunWithUserCultureInfo<T>(string language, Func<T> func)
        {
            var previousCurrentThreadCulture = Thread.CurrentThread.CurrentCulture;
            var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            var previousCurrentCulture = CultureInfo.CurrentCulture;

            var targetCulture = GetCultureInfoFromLanguage(language);

            Thread.CurrentThread.CurrentCulture = targetCulture;
            CultureInfo.DefaultThreadCurrentCulture = targetCulture;
            CultureInfo.CurrentCulture = targetCulture;

            var retVal = func();

            Thread.CurrentThread.CurrentCulture = previousCurrentThreadCulture;
            CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
            CultureInfo.CurrentCulture = previousCurrentCulture;

            return retVal;
        }

        public CultureInfo GetCultureInfoFromLanguage(string language)
        {
            switch (language?.ToLower())
            {
                case "de":
                    return CultureInfo.CreateSpecificCulture("de-CH");
                case "fr":
                    return CultureInfo.CreateSpecificCulture("fr-CH");
                case "en":
                    return CultureInfo.CreateSpecificCulture("en-GB");
                case "it":
                    return CultureInfo.CreateSpecificCulture("it-CH");
                default:
                    return CultureInfo.CreateSpecificCulture("de-CH");
            }
        }
    }
}
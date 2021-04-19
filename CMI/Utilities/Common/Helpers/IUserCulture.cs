using System;
using System.Globalization;

namespace CMI.Utilities.Common.Helpers
{
    public interface IUserCulture
    {
        /// <summary>
        ///     Runs an Action with the cultureinfo of the user set, to display dates and numbers in users language
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="language">users language (ex: "de")</param>
        /// <param name="func">the action that should be run between the cultureinfo set</param>
        /// <returns></returns>
        T RunWithUserCultureInfo<T>(string language, Func<T> func);

        CultureInfo GetCultureInfoFromLanguage(string language);
    }
}
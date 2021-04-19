using System;
using System.Configuration;

namespace CMI.Tools.JsonCombiner
{
    public enum Language
    {
        En,
        It,
        Fr
    }

    public class LanguageUtil
    {
        public static string LanguageToString(Language lng)
        {
            switch (lng)
            {
                case Language.En:
                    return ConfigurationManager.AppSettings.Get(Language.En.ToString());
                case Language.It:
                    return ConfigurationManager.AppSettings.Get(Language.It.ToString());
                case Language.Fr:
                    return ConfigurationManager.AppSettings.Get(Language.Fr.ToString());
                default:
                    throw new Exception("this is no Language");
            }
        }
    }
}
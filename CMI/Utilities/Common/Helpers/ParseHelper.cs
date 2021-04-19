using System;
using System.Globalization;

namespace CMI.Utilities.Common.Helpers
{
    public static class ParseHelper
    {
        public static CultureInfo cultureInfoSwiss = new CultureInfo("de-CH");

        public static DateTime? ParseDateTimeSwiss(this string valueToParse)
        {
            if (DateTime.TryParse(valueToParse, cultureInfoSwiss, DateTimeStyles.None, out var result))
            {
                return result;
            }

            return null;
        }

        public static TimeSpan? ParseTimeSwiss(this string valueToParse)
        {
            if (TimeSpan.TryParse(valueToParse, cultureInfoSwiss, out var result))
            {
                return result;
            }

            return null;
        }
    }
}
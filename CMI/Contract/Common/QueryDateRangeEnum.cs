using System;
using System.ComponentModel;

namespace CMI.Contract.Common
{
    public enum QueryDateRangeEnum
    {
        [Description("Letzte viertel Stunde")] LastQuarterHour,

        [Description("Letzte halbe Stunde")] LastHalfHour,

        [Description("Letzte Stunde")] LastHour,

        [Description("Letzte 2 Stunden")] LastTwoHours,

        [Description("Letzte 8 Stunden")] LastEightHours,

        [Description("Letzte 24 Stunden")] LastTwentyFourHours,

        [Description("Heute")] Today,

        [Description("Gestern")] Yesterday,

        [Description("Diese Woche")] ThisWeek,

        [Description("Letzte Woche")] LastWeek,

        [Description("Diesen Monat")] ThisMonth,

        [Description("Letzten Monat")] LastMonth
    }

    public static class EnumExtension
    {
        public static string GetDescription<T>(this T enumerationValue)
            where T : struct
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            // Tries to find a DescriptionAttribute for a potential friendly name
            // for the enum
            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                    // Pull out the description value
                {
                    return ((DescriptionAttribute) attrs[0]).Description;
                }
            }

            // If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();
        }
    }
}
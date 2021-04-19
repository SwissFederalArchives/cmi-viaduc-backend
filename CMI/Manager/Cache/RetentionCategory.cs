using System;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Cache
{
    public class RetentionCategory
    {
        public RetentionCategory(string s, CacheRetentionCategory cacheRetentionCategory)
        {
            CacheRetentionCategory = cacheRetentionCategory;
            RetentionSpan = CalcTimeSpanFromString(s);
        }

        public CacheRetentionCategory CacheRetentionCategory { get; }
        public TimeSpan RetentionSpan { get; set; }

        private TimeSpan CalcTimeSpanFromString(string s)
        {
            if (s.ToLowerInvariant() == "infinite")
            {
                return TimeSpan.MaxValue;
            }

            switch (s.Last())
            {
                case 'y':
                    return TimeSpan.FromDays(365 * GetNumericPart(s));

                case 'd':
                    return TimeSpan.FromDays(GetNumericPart(s));

                case 'h':
                    return TimeSpan.FromHours(GetNumericPart(s));

                case 'm':
                    return TimeSpan.FromMinutes(GetNumericPart(s));

                case 's':
                    return TimeSpan.FromSeconds(GetNumericPart(s));

                default:
                    throw new Exception("RetentionCategory has invalid format. Please use something like '59s', '5m', '12h', '30d', '25y'.");
            }
        }

        private int GetNumericPart(string s)
        {
            return int.Parse(s.Substring(0, s.Length - 1));
        }
    }
}
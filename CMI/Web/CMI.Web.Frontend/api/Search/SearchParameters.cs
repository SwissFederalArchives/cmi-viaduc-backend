using System;
using System.Text.RegularExpressions;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Search
{
    public class DateRange
    {
        public static DateRange ToDateRange(JObject obj)
        {
            var type = JsonHelper.GetTokenValue<string>(obj, "type") ?? JsonHelper.GetTokenValue<string>(obj, "Type");
            var start = JsonHelper.GetTokenValue<string>(obj, "start") ?? JsonHelper.GetTokenValue<string>(obj, "Start");
            var end = JsonHelper.GetTokenValue<string>(obj, "end") ?? JsonHelper.GetTokenValue<string>(obj, "End");
            var range = new DateRange
            {
                Type = type,
                Start = FromValue(start),
                End = FromValue(end, "custom".Equals(type))
            };
            return range;
        }

        public static DateTime? FromValue(string value, bool makeUpperBound = false)
        {
            DateTime? date = null;

            if (!string.IsNullOrEmpty(value))
            {
                DateTime d;
                if (DateTime.TryParse(value, out d))
                {
                    date = d;
                }
                else
                {
                    try
                    {
                        Match m;
                        if ((m = TTMMJJJJMatcher.Match(value)).Success)
                        {
                            date = new DateTime(Convert.ToInt32(m.Groups["year"].Value), Convert.ToInt32(m.Groups["month"].Value),
                                Convert.ToInt32(m.Groups["day"].Value));
                            if (makeUpperBound)
                            {
                                date = date.Value.AddDays(1);
                            }
                        }
                        else if ((m = MMJJJJMatcher.Match(value)).Success)
                        {
                            date = new DateTime(Convert.ToInt32(m.Groups["year"].Value), Convert.ToInt32(m.Groups["month"].Value), 1);
                            if (makeUpperBound)
                            {
                                date = date.Value.AddMonths(1);
                            }
                        }
                        else if ((m = JJJJMatcher.Match(value)).Success)
                        {
                            date = new DateTime(Convert.ToInt32(m.Groups["year"].Value), 1, 1);
                            if (makeUpperBound)
                            {
                                date = date.Value.AddYears(1);
                            }
                        }
                    }
                    catch
                    {
                        date = null; // ignore here
                    }
                }
            }

            return date;
        }

        #region Properties

        public string Type { get; set; }

        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        #endregion


        #region Globals

        private static readonly Regex TTMMJJJJMatcher = new Regex(@"(?<days>\d{1,2})[.-](?<month>\d{1,2})[.-](?<year>\d{1,4})");
        private static readonly Regex MMJJJJMatcher = new Regex(@"(?<month>\d{1,2})[.-](?<year>\d{1,4})");
        private static readonly Regex JJJJMatcher = new Regex(@"(?<year>\d{1,4})");

        #endregion
    }


    public class FacetFilters
    {
        public string Facet { get; set; }

        public string[] Filters { get; set; }
    }

    public class SearchOptions
    {
        public bool EnableExplanations { get; set; }
        public bool EnableHighlighting { get; set; } = true;
        public bool EnableAggregations { get; set; } = true;
    }


    public class SearchParameters
    {
        public SearchModel Query { get; set; }
        public FacetFilters[] FacetsFilters { get; set; }
        public Paging Paging { get; set; }
        public SearchOptions Options { get; set; }

        public CaptchaVerificationData Captcha { get; set; }
    }
}
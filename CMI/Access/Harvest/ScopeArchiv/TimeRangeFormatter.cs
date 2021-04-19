using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using CMI.Access.Harvest.Properties;

namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     Formatiert Datum und Zeitraum im scope-Standardformat in ein sprach- und länderabhängiges
    ///     Textformat.
    /// </summary>
    public sealed class TimeRangeFormatter
    {
        private readonly string centuryPattern;
        private readonly CultureInfo cultureInfo;
        private readonly ResourceManager resourceManager;
        private readonly string yearMonthDayPattern;
        private readonly string yearMonthPattern;
        private readonly string yearPattern;

        #region Constructors

        /// <summary>
        ///     Class for formatting date values given the scope specific
        ///     standard date info
        /// </summary>
        /// <param name="cultureInfo">The culture information.</param>
        public TimeRangeFormatter(CultureInfo cultureInfo)
        {
            this.cultureInfo = cultureInfo;
            resourceManager = new ResourceManager(typeof(Resources));

            yearMonthDayPattern = GetYearMonthDayPattern();
            yearMonthPattern = GetYearMonthPattern();
            yearPattern = GetYearPattern();
            centuryPattern = GetCenturyPattern();
        }

        #endregion

        /// <summary>
        ///     Formatiert einen Zeitraum in ein länderspezifisches Textformat.
        /// </summary>
        /// <param name="dateOperator">Der Zeitraumsoperator.</param>
        /// <param name="bgnDtStd">Der Von-Datumswert im scopeArchiv-Standardformat.</param>
        /// <param name="endDtStd">Der Bis-Datumswert im scopeArchiv-Standardformat.</param>
        /// <param name="bgnCirca">Der Circa-Indikator beim Von-Datumswert.</param>
        /// <param name="endCirca">Der Circa-Indikator beim Bis-Datumswert.</param>
        /// <returns></returns>
        public string Format(ScopeArchivDateOperator dateOperator, string bgnDtStd, string endDtStd, bool bgnCirca, bool endCirca)
        {
            string format;
            var von = "";
            var bis = "";

            switch (dateOperator)
            {
                case ScopeArchivDateOperator.Between:
                    format = resourceManager.GetString("TimeRangeFormatter_Zwischen", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    bis = Format(endDtStd, endCirca);
                    break;

                case ScopeArchivDateOperator.FromTo:
                    format = resourceManager.GetString("TimeRangeFormatter_VonBis", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    bis = Format(endDtStd, endCirca);
                    break;

                case ScopeArchivDateOperator.After:
                    format = resourceManager.GetString("TimeRangeFormatter_Nach", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    break;

                case ScopeArchivDateOperator.From:
                    format = resourceManager.GetString("TimeRangeFormatter_Ab", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    break;

                case ScopeArchivDateOperator.Before:
                    format = resourceManager.GetString("TimeRangeFormatter_Vor", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    break;

                case ScopeArchivDateOperator.To:
                    format = resourceManager.GetString("TimeRangeFormatter_Bis", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    break;

                case ScopeArchivDateOperator.SineDato:
                    format = resourceManager.GetString("TimeRangeFormatter_SineDato", cultureInfo);
                    break;

                case ScopeArchivDateOperator.Exact:
                    format = resourceManager.GetString("TimeRangeFormatter_Genau", cultureInfo);
                    von = Format(bgnDtStd, bgnCirca);
                    break;

                case ScopeArchivDateOperator.None:
                    format = resourceManager.GetString("TimeRangeFormatter_KeineAngabe", cultureInfo);
                    break;

                default:
                    format = "";
                    break;
            }

            Debug.Assert(format != null, "format != null");
            return string.Format(format, von, bis);
        }

        /// <summary>
        ///     Formatiert ein Datum in ein länderspezifisches Textformat.
        /// </summary>
        /// <param name="dtStd">Der Datumswert im scopeArchiv-Standardformat.</param>
        /// <param name="circa">Der Circa-Indikator.</param>
        /// <returns></returns>
        public string Format(string dtStd, bool circa)
        {
            string pattern;
            var century = 0;
            var year = 0;
            var month = 0;
            var day = 0;

            switch (dtStd.Length)
            {
                case 3: // Jahrhundert
                    pattern = centuryPattern;
                    century = int.Parse(dtStd.Substring(1));
                    break;

                case 5: // Jahr
                    pattern = yearPattern;
                    year = int.Parse(dtStd.Substring(1, 4));
                    break;

                case 7: // Jahr/Monat
                    pattern = yearMonthPattern;
                    year = int.Parse(dtStd.Substring(1, 4));
                    month = int.Parse(dtStd.Substring(5, 2));
                    break;

                case 9: // Jahr/Monat/Tag
                    pattern = yearMonthDayPattern;
                    year = int.Parse(dtStd.Substring(1, 4));
                    month = int.Parse(dtStd.Substring(5, 2));
                    day = int.Parse(dtStd.Substring(7, 2));
                    break;

                default:
                    return string.Empty;
            }

            var result = string.Format(pattern, century, year, month, day);

            if (dtStd[0] == '-')
            {
                if (dtStd.Length == 3) // Jahrhundert
                {
                    result = string.Format(resourceManager.GetString("TimeRangeFormatter_VorChristus", cultureInfo) ?? "", result);
                }
                else
                {
                    result = "-" + result;
                }
            }

            if (circa)
            {
                result = string.Format(resourceManager.GetString("TimeRangeFormatter_Circa", cultureInfo) ?? "", result);
            }

            return result;
        }

        private string GetYearMonthDayPattern()
        {
            var pattern = !cultureInfo.IsNeutralCulture ? cultureInfo.DateTimeFormat.ShortDatePattern : "yyyy.MM.dd";

            pattern = Regex.Replace(pattern, @"y+", @"{1:0000}");
            pattern = Regex.Replace(pattern, @"M{2,}", @"{2:00}");
            pattern = Regex.Replace(pattern, @"M+", @"{2:0}");
            pattern = Regex.Replace(pattern, @"d{2,}", @"{3:00}");
            pattern = Regex.Replace(pattern, @"d+", @"{3:0}");

            return pattern;
        }

        private string GetYearMonthPattern()
        {
            string pattern;
            string separator;

            if (!cultureInfo.IsNeutralCulture)
            {
                pattern = cultureInfo.DateTimeFormat.ShortDatePattern;
                separator = cultureInfo.DateTimeFormat.DateSeparator;
            }
            else
            {
                pattern = "yyyy.MM.dd";
                separator = ".";
            }

            var dayRemovePattern = string.Format(@"(\{0}d+|d+{0})", separator);
            pattern = Regex.Replace(pattern, dayRemovePattern, @"");

            pattern = Regex.Replace(pattern, @"y+", @"{1:0000}");
            pattern = Regex.Replace(pattern, @"M{2,}", @"{2:00}");
            pattern = Regex.Replace(pattern, @"M+", @"{2:0}");

            return pattern;
        }

        private string GetYearPattern()
        {
            return @"{1:0000}";
        }

        private string GetCenturyPattern()
        {
            return resourceManager.GetString("TimeRangeFormatter_Century", cultureInfo);
        }
    }
}
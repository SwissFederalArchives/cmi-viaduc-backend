using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using CMI.Access.Harvest.Properties;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public static class DataElementHelper
    {
        public static void FillDataElementElementValue(DataElementElementType elementType, DetailDataDataSet.DetailDataRow row,
            DataElementElementValue value,
            LanguageSettings languageSettings)
        {
            try
            {
                value.Sequence = row.IsELMNT_SQNZ_NRNull() ? 1 : (int) row.ELMNT_SQNZ_NR;
                switch (elementType)
                {
                    case DataElementElementType.text:
                        value.TextValues.Add(GetDefaultLanguageMemoText(row, languageSettings.DefaultLanguage));
                        break;
                    case DataElementElementType.memo:
                        // Memo text is appended to existing text and sequence is always 1
                        value.Sequence = 1;
                        var existing = value.TextValues.FirstOrDefault();
                        if (existing == null)
                        {
                            existing = new DataElementElementValueTextValue
                            {
                                Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                                IsDefaultLang = true
                            };
                            value.TextValues.Add(existing);
                        }

                        existing.Value += row.MEMO_TXT;
                        break;
                    case DataElementElementType.selection:
                        value.TextValues.Add(GetDefaultLanguageMemoText(row, languageSettings.DefaultLanguage));
                        break;
                    case DataElementElementType.date:
                        value.TextValues = GetDateTranslations(row.BGN_DT_STND, languageSettings);
                        value.DateRange = new DateRange
                        {
                            From = row.BGN_DT_STND,
                            To = row.END_DT_STND,
                            FromApproxIndicator = row.BGN_CIRCA_IND != 0,
                            ToApproxIndicator = row.BGN_CIRCA_IND != 0,
                            FromDate = row.BGN_DT,
                            ToDate = row.END_DT,
                            DateOperator = MapperHelper.MapDateOperator((ScopeArchivDateOperator) row.DT_OPRTR_ID)
                        };
                        // Special logic for search dates.
                        var searchDates = GetSearchDate(row.BGN_DT_STND, row.BGN_DT, row.BGN_CIRCA_IND != 0, row.BGN_DT_STND, row.END_DT,
                            row.BGN_CIRCA_IND != 0);
                        value.DateRange.SearchFromDate = searchDates.FromDate;
                        value.DateRange.SearchToDate = searchDates.ToDate;

                        break;
                    case DataElementElementType.datePrecise:
                        value.TextValues = GetDateTranslations(row.BGN_DT_STND, languageSettings);
                        value.DateValue = row.BGN_DT;
                        break;
                    case DataElementElementType.dateRange:
                        value.TextValues = GetDateRangeTranslations((ScopeArchivDateOperator) row.DT_OPRTR_ID, row.BGN_DT_STND, row.END_DT_STND,
                            row.BGN_CIRCA_IND != 0, row.END_CIRCA_IND != 0,
                            languageSettings);
                        value.DateRange = new DateRange
                        {
                            From = row.BGN_DT_STND,
                            To = row.END_DT_STND,
                            FromApproxIndicator = row.BGN_CIRCA_IND != 0,
                            ToApproxIndicator = row.END_CIRCA_IND != 0,
                            FromDate = row.BGN_DT,
                            ToDate = row.END_DT,
                            DateOperator = MapperHelper.MapDateOperator((ScopeArchivDateOperator) row.DT_OPRTR_ID)
                        };
                        // Special logic for search dates.
                        searchDates = GetSearchDate(row.BGN_DT_STND, row.BGN_DT, row.BGN_CIRCA_IND != 0, row.END_DT_TXT, row.END_DT,
                            row.END_CIRCA_IND != 0);
                        value.DateRange.SearchFromDate = searchDates.FromDate;
                        value.DateRange.SearchToDate = searchDates.ToDate;

                        break;
                    case DataElementElementType.integer:
                        value.IntValue = (int) row.INT_ZAHL;
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.INT_ZAHL.ToString("N0", languageSettings.DefaultLanguage),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        foreach (var language in languageSettings.SupportedLanguages)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = row.INT_ZAHL.ToString("N0", language),
                                Lang = language.TwoLetterISOLanguageName
                            });
                        }

                        break;
                    case DataElementElementType.@float:
                        value.FloatValue = new DataElementElementValueFloatValue
                        {
                            Value = (float) row.FLOAT_ZAHL,
                            DecimalPositions = (int) row.DZML_STLN_ANZ
                        };
                        var frmtString = "N" + row.DZML_STLN_ANZ;
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.FLOAT_ZAHL.ToString(frmtString, languageSettings.DefaultLanguage),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        foreach (var language in languageSettings.SupportedLanguages)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = row.FLOAT_ZAHL.ToString(frmtString, language),
                                Lang = language.TwoLetterISOLanguageName
                            });
                        }

                        break;
                    case DataElementElementType.boolean:
                        var rm = new ResourceManager(typeof(Resources));
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = (int) row.INT_ZAHL == 0
                                ? rm.GetString("BooleanNo", languageSettings.DefaultLanguage)
                                : rm.GetString("BooleanYes", languageSettings.DefaultLanguage),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        foreach (var language in languageSettings.SupportedLanguages)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = (int) row.INT_ZAHL == 0 ? rm.GetString("BooleanNo", language) : rm.GetString("BooleanYes", language),
                                Lang = language.TwoLetterISOLanguageName
                            });
                        }

                        value.BooleanValue = row.INT_ZAHL != 0;
                        break;
                    case DataElementElementType.time:
                        value.TimeValue = row.ZT;
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.ZT.ToString("T", languageSettings.DefaultLanguage),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        foreach (var language in languageSettings.SupportedLanguages)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = row.ZT.ToString("T", language),
                                Lang = language.TwoLetterISOLanguageName
                            });
                        }

                        break;
                    case DataElementElementType.timespan:
                        var seconds = (int) row.INT_ZAHL;
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = TimeSpan.FromSeconds(seconds).ToString("g", languageSettings.DefaultLanguage),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        foreach (var language in languageSettings.SupportedLanguages)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = TimeSpan.FromSeconds(seconds).ToString("g", language),
                                Lang = language.TwoLetterISOLanguageName
                            });
                        }

                        value.DurationInSeconds = seconds;
                        break;
                    case DataElementElementType.hyperlink:
                        var linkParts = row.MEMO_TXT.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                        if (linkParts.Length > 1)
                        {
                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = linkParts[0],
                                Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                                IsDefaultLang = true
                            });
                            value.Link = new DataElementElementValueLink
                            {
                                Href = linkParts[1].ToUpper().StartsWith("HTTP://") ? linkParts[1] :
                                    linkParts[1].ToUpper().StartsWith("HTTPS://") ? linkParts[1] : "http://" + linkParts[1],
                                Value = linkParts[0]
                            };
                        }
                        else
                        {
                            var link = row.MEMO_TXT;
                            link = link.ToUpper().StartsWith("HTTP://") ? link :
                                link.ToUpper().StartsWith("HTTPS://") ? link : "http://" + linkParts[0];

                            value.TextValues.Add(new DataElementElementValueTextValue
                            {
                                Value = row.MEMO_TXT,
                                Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                                IsDefaultLang = true
                            });
                            value.Link = new DataElementElementValueLink
                            {
                                Href = link,
                                Value = row.MEMO_TXT
                            };
                        }

                        break;
                    case DataElementElementType.header:
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.TITEL,
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        break;
                    case DataElementElementType.entityLink:
                        value.TextValues.Add(GetDefaultLanguageMemoText(row, languageSettings.DefaultLanguage));
                        value.EntityLink = new DataElementElementValueEntityLink
                        {
                            EntityRecordId = row.VRKNP_GSFT_OBJ_ID.ToString("F0"),
                            EntityType = Enum.GetName(typeof(ScopeArchivGeschaeftsObjektKlasse), (int) row.VRKNP_GSFT_OBJ_KLS_ID),
                            Value = row.MEMO_TXT
                        };
                        break;
                    case DataElementElementType.accrual:
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.INT_ZAHL + " - " + row.MEMO_TXT,
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        break;
                    case DataElementElementType.fileLink:
                        value.TextValues.Add(GetDefaultLanguageMemoText(row, languageSettings.DefaultLanguage));
                        break;
                    case DataElementElementType.mailLink:
                        var mailLink = row.MEMO_TXT;
                        mailLink = mailLink.ToUpper().StartsWith("MAILTO:") ? mailLink : "mailto:" + mailLink;

                        value.TextValues.Add(GetDefaultLanguageMemoText(row, languageSettings.DefaultLanguage));
                        value.Link = new DataElementElementValueLink
                        {
                            Href = mailLink,
                            Value = row.MEMO_TXT
                        };
                        break;
                    case DataElementElementType.image:
                    case DataElementElementType.media:
                        value.TextValues.Add(new DataElementElementValueTextValue
                        {
                            Value = row.MEMO_TXT.Substring(row.MEMO_TXT.LastIndexOf("\\", StringComparison.Ordinal) + 1),
                            Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                            IsDefaultLang = true
                        });
                        value.BlobValueBase64 = GetBlobValue(row.BNR_DATEN);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(elementType), elementType, null);
                }
            }
            catch (Exception ex)
            {
                // Simply log the error and then return the value as is.
                // This simply means that the archive record might miss one data value
                Log.Error(ex, "Failed to fill the data element value for {@Row} of element type {@ElementType}", row, elementType);
            }
        }

        public static List<DataElementElementValueTextValue> GetDateTranslations(string standardDate, LanguageSettings languageSettings)
        {
            var retVal = new List<DataElementElementValueTextValue>();
            // Add default language
            var trf = new TimeRangeFormatter(languageSettings.DefaultLanguage);
            retVal.Add(new DataElementElementValueTextValue
            {
                Value = trf.Format(standardDate, false),
                Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                IsDefaultLang = true
            });

            // Add supported languages
            foreach (var cultureInfo in languageSettings.SupportedLanguages)
            {
                trf = new TimeRangeFormatter(cultureInfo);
                retVal.Add(new DataElementElementValueTextValue
                {
                    Value = trf.Format(standardDate, false),
                    Lang = cultureInfo.TwoLetterISOLanguageName
                });
            }

            return retVal;
        }

        public static List<DataElementElementValueTextValue> GetDateRangeTranslations(ScopeArchivDateOperator dateOperator, string standardDateFrom,
            string standardDateTo, bool approxFlagFrom,
            bool approxFlagTo, LanguageSettings languageSettings)
        {
            var retVal = new List<DataElementElementValueTextValue>();
            // Add default language
            var trf = new TimeRangeFormatter(languageSettings.DefaultLanguage);
            retVal.Add(new DataElementElementValueTextValue
            {
                Value = trf.Format(dateOperator, standardDateFrom, standardDateTo, approxFlagFrom, approxFlagTo),
                Lang = languageSettings.DefaultLanguage.TwoLetterISOLanguageName,
                IsDefaultLang = true
            });

            // Add supported languages
            foreach (var cultureInfo in languageSettings.SupportedLanguages)
            {
                trf = new TimeRangeFormatter(cultureInfo);
                retVal.Add(new DataElementElementValueTextValue
                {
                    Value = trf.Format(dateOperator, standardDateFrom, standardDateTo, approxFlagFrom, approxFlagTo),
                    Lang = cultureInfo.TwoLetterISOLanguageName
                });
            }

            return retVal;
        }

        /// <summary>
        ///     Gets the search date based on an input date and some criterias.
        ///     Some handmade logic is applied to get a more "natural" start or end date
        ///     that should be used for searches
        /// </summary>
        /// <param name="startDateStd">
        ///     The start date in the standard scope format.
        ///     +12         --&gt; 12th century
        ///     +1950       --&gt; year 1950
        ///     +195005     --&gt; May 1950
        ///     +19500515   --  15. May 1950
        /// </param>
        /// <param name="startDate">The from date that we are to alter</param>
        /// <param name="startIsApprox">if set to <c>true</c>, then the given date information is only approximative</param>
        /// <param name="endDateStd">The end date in the standard scope format.</param>
        /// <param name="endDate">The from date that we are to alter</param>
        /// <param name="endIsApprox">if set to <c>true</c>, then the given date information is only approximative</param>
        /// <returns>DateTime.</returns>
        public static SearchDateTime GetSearchDate(string startDateStd, DateTime startDate, bool startIsApprox,
            string endDateStd, DateTime endDate, bool endIsApprox)
        {
            // How exact is the date that is given?
            // Precision can be null, 2, 4, 6 or 8 where 8 is the most precise date. Null is possible with sine dato.
            var startPrecision = string.IsNullOrEmpty(startDateStd) ? 8 : startDateStd.Length - 1;
            var endPrecision = string.IsNullOrEmpty(endDateStd) ? startPrecision : endDateStd.Length - 1;

            var startVariance = GetVariance(startPrecision, startIsApprox);
            var endVariance = GetVariance(endPrecision, endIsApprox);

            return new SearchDateTime
            {
                FromDate = startDate.Ticks - startVariance.Ticks > 0 ? startDate.AddDays(startVariance.Days * -1) : DateTime.MinValue,
                ToDate = endDate.Ticks + endVariance.Ticks < DateTime.MaxValue.Date.Ticks ? endDate.AddDays(endVariance.Days) : DateTime.MaxValue.Date
            };
        }

        private static TimeSpan GetVariance(int precision, bool isApprox)
        {
            var variance = TimeSpan.Zero;
            switch (precision)
            {
                case 2:
                    variance = TimeSpan.FromDays(365 * 50); // half a century
                    break;
                case 4:
                    variance = TimeSpan.FromDays(180); // half a year
                    break;
                case 6:
                    variance = TimeSpan.FromDays(15); // half a month
                    break;
                case 8:
                    variance = TimeSpan.Zero; // no variance
                    break;
            }

            // if it is even approximate, we multiply by the precision
            // so the variance is 100 years, 2 years or 3 months depending on precision
            if (isApprox)
            {
                variance = TimeSpan.FromTicks(variance.Ticks * precision);
            }

            return variance;
        }

        private static DataElementElementValueTextValue GetDefaultLanguageMemoText(DetailDataDataSet.DetailDataRow row, CultureInfo defaultCulture)
        {
            return new DataElementElementValueTextValue
            {
                Value = row.MEMO_TXT,
                Lang = defaultCulture.TwoLetterISOLanguageName,
                IsDefaultLang = true
            };
        }

        private static DataElementElementValueBlobValueBase64 GetBlobValue(byte[] bytes)
        {
            var retVal = new DataElementElementValueBlobValueBase64();
            if (bytes.Length > 0)
            {
                retVal.Value = Convert.ToBase64String(bytes);
                retVal.MimeType = BlobHelper.GetMimeFromBytes(bytes);
            }

            return retVal;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class TimePeriodExtractor : ExtractorBase<ElasticTimePeriod>
    {
        protected override ElasticTimePeriod GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticTimePeriod retVal = null;

            if (value != null &&
                value.DateRange.DateOperator != DateRangeDateOperator.na &&
                value.DateRange.DateOperator != DateRangeDateOperator.sd)
            {
                retVal = new ElasticTimePeriod
                {
                    StartDate = value.DateRange.FromDate,
                    EndDate = value.DateRange.ToDate <= DateTime.MaxValue.AddDays(-1)
                        ? value.DateRange.ToDate.AddDays(1).AddSeconds(-1)
                        : DateTime.MaxValue,
                    SearchStartDate = value.DateRange.SearchFromDate,
                    SearchEndDate = value.DateRange.SearchToDate,
                    Text = value.TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value,
                    StartDateText = value.DateRange.From,
                    EndDateText = value.DateRange.To,
                    StartDateApproxIndicator = value.DateRange.FromApproxIndicator,
                    EndDateApproxIndicator = value.DateRange.ToApproxIndicator
                };

                retVal.Years = GetCreationPeriodYears(value, retVal.StartDate, retVal.EndDate);
            }

            return retVal;
        }

        private static List<int> GetCreationPeriodYears(DataElementElementValue value, DateTime startDate, DateTime endDate)
        {
            // Calculate the number of years
            int lastYear;
            if (endDate.Month == 1 &&
                endDate.Day == 1 &&
                endDate.Hour == 0 &&
                endDate.Minute == 0 &&
                endDate.Second == 0 &&
                endDate.Millisecond == 0)
            {
                lastYear = endDate.Year - 1;
            }
            else
            {
                lastYear = endDate.Year;
            }

            var firstYear = startDate.Year;
            if (value.DateRange.DateOperator == DateRangeDateOperator.after ||
                value.DateRange.DateOperator == DateRangeDateOperator.startingWith)
            {
                lastYear = firstYear;
            }

            if (value.DateRange.DateOperator == DateRangeDateOperator.to ||
                value.DateRange.DateOperator == DateRangeDateOperator.before)
            {
                firstYear = lastYear;
            }

            var years = Enumerable.Range(firstYear, lastYear - firstYear + 1).ToList();
            return years;
        }
    }
}
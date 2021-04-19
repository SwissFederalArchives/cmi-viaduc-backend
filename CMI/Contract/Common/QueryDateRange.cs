using System;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     The query date range can be used for queries when a from to date is required.
    ///     The class provides some common date ranges as an enum for getting the dates.
    /// </summary>
    public class QueryDateRange
    {
        public QueryDateRange(QueryDateRangeEnum dateRange)
        {
            switch (dateRange)
            {
                case QueryDateRangeEnum.LastQuarterHour:
                    From = DateTime.Now.AddMinutes(-15);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastHalfHour:
                    From = DateTime.Now.AddMinutes(-30);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastHour:
                    From = DateTime.Now.AddMinutes(-60);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastTwoHours:
                    From = DateTime.Now.AddMinutes(-120);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastEightHours:
                    From = DateTime.Now.AddHours(-8);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastTwentyFourHours:
                    From = DateTime.Now.AddHours(-24);
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.Today:
                    From = DateTime.Today;
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.Yesterday:
                    From = DateTime.Today.AddDays(-1);
                    To = DateTime.Today.AddSeconds(-1);
                    break;
                case QueryDateRangeEnum.ThisWeek:
                    From = DateTime.Today.AddDays(-1 * (DateTime.Today.DayOfWeek == DayOfWeek.Sunday
                        ? 6
                        : (int) DateTime.Today.DayOfWeek + (int) DayOfWeek.Monday));
                    To = DateTime.Now;
                    break;
                case QueryDateRangeEnum.LastWeek:
                    From = DateTime.Today.AddDays(-1 *
                                                  ((DateTime.Today.DayOfWeek == DayOfWeek.Sunday
                                                      ? 6
                                                      : (int) DateTime.Today.DayOfWeek + (int) DayOfWeek.Monday) + 7));
                    To = From.AddDays(7).AddSeconds(-1);
                    break;
                case QueryDateRangeEnum.ThisMonth:
                    From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    To = From.AddMonths(1).AddSeconds(-1);
                    break;
                case QueryDateRangeEnum.LastMonth:
                    From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                    To = From.AddMonths(1).AddSeconds(-1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dateRange), dateRange, null);
            }
        }

        public DateTime From { get; }

        public DateTime To { get; }
    }
}
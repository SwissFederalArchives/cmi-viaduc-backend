using System;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public class SipDateBuilder
    {
        private const int StandardDatePrecisionCentury = 3;
        private const int StandardDatePrecisionYear = 5;
        private const int StandardDatePrecisionYearMonth = 7;
        private const int StandardDatePrecisionYearMonthDay = 9;
        private const string ArgumentExceptionMessage = "Es sind nur Datumsangaben im Format JJJJ oder TT.MM.JJJJ erlaubt.";
        private const string ArgumentOpenDatenRangesExceptionMessage = "Es sind keine 'offenen' Zeiträume wie < 1900 oder > 2000 erlaubt.";

        public string ConvertToValidSipDateString(string fromDate, bool fromApproxIndicator, string toDate, bool toApproxIndicator,
            ScopeArchivDateOperator? dateOperator, string noDataAvailable)
        {
            // If date operator is null, then no date has been specified
            if (!dateOperator.HasValue)
            {
                return noDataAvailable;
            }

            // According to customer, the following dates are not allowed and must raise an exception
            // +YY and +YYYYMM
            // FromDates are null when operator "sine dato" or "k.A." is used
            if (!string.IsNullOrEmpty(fromDate) &&
                (fromDate.Length == StandardDatePrecisionCentury || fromDate.Length == StandardDatePrecisionYearMonth))
            {
                throw new ArgumentOutOfRangeException(nameof(fromDate), ArgumentExceptionMessage);
            }

            // ToDate can be null. For example with exact dates, only the from date is given.
            if (!string.IsNullOrEmpty(toDate) && (toDate.Length == StandardDatePrecisionCentury || toDate.Length == StandardDatePrecisionYearMonth))
            {
                throw new ArgumentOutOfRangeException(nameof(toDate), ArgumentExceptionMessage);
            }

            // Gemäss Mail: Christa Ackermann sollen "offene" Zeiträume als Fehler behandelt werden. 
            // Wir lassen jedoch die Formatierung dieser Zeiträume im zweiten Switch Statement drin, falls sich die Meinungen ändern.
            switch (dateOperator)
            {
                case ScopeArchivDateOperator.After:
                case ScopeArchivDateOperator.From:
                    throw new ArgumentOutOfRangeException(nameof(toDate), ArgumentOpenDatenRangesExceptionMessage);
                case ScopeArchivDateOperator.Before:
                case ScopeArchivDateOperator.To:
                    throw new ArgumentOutOfRangeException(nameof(fromDate), ArgumentOpenDatenRangesExceptionMessage);
            }


            // Depending on the date type, we format our date string
            /*
                DT_OPRTR_ID  DT_OPRTR_CD  DT_OPRTR_NM             DT_OPRTR_ANZG_TXT     OPRTR_ANZ          
                1            Zwischen     Zwischen (> x und < y)  %1-%2                 2                  
                2            Von / Bis    Von/Bis (>= x und <=y)  %1-%2                 2                  
                3            >            Nach (>)                %1-                   1                  
                4            >=           Ab (>=)                 %1-                   1                  
                5            <            Vor (<)                 -%1                   1                  
                6            <=           Bis (<=)                -%1                   1                  
                7            ohne         s.d. (sine dato)        s.d. (sine dato)      0                  
                8            Genau (=)    Genau (=)               %1                    1                  
                9            k.A.         keine Angabe            keine Angabe          0                  
             */
            switch (dateOperator.Value)
            {
                case ScopeArchivDateOperator.Between:
                case ScopeArchivDateOperator.FromTo:
                    CheckFromDateIsBeforeToDate(fromDate, toDate);
                    return $"{ToSipDate(fromDate, fromApproxIndicator)}-{ToSipDate(toDate, toApproxIndicator)}";
                case ScopeArchivDateOperator.After:
                case ScopeArchivDateOperator.From:
                    return $"{ToSipDate(fromDate, fromApproxIndicator)}-{ToSipDate("+99991231", toApproxIndicator)}";
                case ScopeArchivDateOperator.Before:
                case ScopeArchivDateOperator.To:
                    // In scope the "from date" always contains the date, even in this case
                    return $"{ToSipDate("+00010101", fromApproxIndicator)}-{ToSipDate(fromDate, toApproxIndicator)}";
                case ScopeArchivDateOperator.Exact:
                    return $"{ToSipDate(fromDate, fromApproxIndicator)}-{ToSipDate(fromDate, fromApproxIndicator)}";
                case ScopeArchivDateOperator.None:
                case ScopeArchivDateOperator.SineDato:
                    return noDataAvailable;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dateOperator), dateOperator, null);
            }
        }

        private void CheckFromDateIsBeforeToDate(string fromDate, string toDate)
        {
            var date1 = ToDate(fromDate, false);
            var date2 = ToDate(toDate, true);

            if (date1 > date2)
            {
                throw new InvalidOperationException("Das Startdatum ist grösser als das Enddatum. Dies ist nicht korrekt.");
            }
        }

        private string ToSipDate(string date, bool approxIndicator)
        {
            string retVal;
            switch (date.Length)
            {
                case StandardDatePrecisionYear:
                    // Only use the year part. Cut off the plus/minus sign that would indicate BC dates.
                    // We assume only AD dates 
                    retVal = date.Substring(1);
                    break;
                case StandardDatePrecisionYearMonthDay:
                    var year = date.Substring(1, 4);
                    var month = date.Substring(5, 2);
                    var day = date.Substring(7, 2);
                    retVal = $"{day}.{month}.{year}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(date), ArgumentExceptionMessage);
            }

            // Add approx indicator 
            if (approxIndicator)
            {
                retVal = $"ca.{retVal}";
            }

            return retVal;
        }

        private DateTime ToDate(string date, bool isToDate)
        {
            DateTime retVal;
            switch (date.Length)
            {
                case StandardDatePrecisionYear:
                    return isToDate
                        ? new DateTime(Convert.ToInt32(date.Substring(1)), 12, 31)
                        : new DateTime(Convert.ToInt32(date.Substring(1)), 1, 1);
                case StandardDatePrecisionYearMonthDay:
                    var year = Convert.ToInt32(date.Substring(1, 4));
                    var month = Convert.ToInt32(date.Substring(5, 2));
                    var day = Convert.ToInt32(date.Substring(7, 2));
                    retVal = new DateTime(year, month, day);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(date), ArgumentExceptionMessage);
            }

            return retVal;
        }
    }
}
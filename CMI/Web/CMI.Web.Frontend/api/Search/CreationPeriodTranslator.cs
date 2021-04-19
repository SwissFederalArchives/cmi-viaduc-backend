using System;
using CMI.Access.Sql.Viaduc;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public class CreationPeriodTranslator : IFieldTranslator
    {
        public QueryContainer CreateQueryForField(SearchField field, UserAccess access)
        {
            var startDateKey = "creationPeriod.startDate";
            var endDateKey = "creationPeriod.endDate";
            var originalQuery = field.Value;

            var values = originalQuery.Split(new[] {"-"}, StringSplitOptions.None);

            if (values.Length == 1)
            {
                values = new[] {values[0], values[0]};
            }

            if (values.Length != 2)
            {
                throw new InvalidOperationException("The creation period must contain zero or one – character");
            }

            var searchFromString = values[0].Trim();
            var searchToString = values[1].Trim();

            var searchFrom = ConvertFrom(searchFromString);
            var searchTo = ConvertTo(searchToString);

            if (searchFrom > searchTo)
            {
                searchFrom = ConvertFrom(searchToString);
                searchTo = ConvertTo(searchFromString);
            }

            var startQuery = new DateRangeQuery
            {
                Field = startDateKey,
                Format = "dd.MM.yyyy",
                LessThanOrEqualTo = searchTo.ToString("dd.MM.yyyy")
            };
            var endQuery = new DateRangeQuery
            {
                Field = endDateKey,
                Format = "dd.MM.yyyy",
                GreaterThanOrEqualTo = searchFrom.ToString("dd.MM.yyyy")
            };

            return new BoolQuery {Must = new QueryContainer[] {startQuery, endQuery}};
        }


        private static DateTime ConvertFrom(string sucheVonString)
        {
            if (string.IsNullOrWhiteSpace(sucheVonString))
            {
                return new DateTime(1, 1, 1);
            }

            if (sucheVonString.Length == 4)
            {
                return new DateTime(int.Parse(sucheVonString), 1, 1);
            }

            var parts = sucheVonString.Split('.');

            if (parts.Length == 2)
            {
                return new DateTime(int.Parse(parts[1]), int.Parse(parts[0]), 1);
            }

            return new DateTime(int.Parse(parts[2]), int.Parse(parts[1]), int.Parse(parts[0]));
        }

        private static DateTime ConvertTo(string sucheBisString)
        {
            if (string.IsNullOrWhiteSpace(sucheBisString))
            {
                return new DateTime(9999, 12, 31);
            }

            if (sucheBisString.Length == 4)
            {
                return new DateTime(int.Parse(sucheBisString), 12, 31);
            }

            var parts = sucheBisString.Split('.');

            if (parts.Length == 2)
            {
                return new DateTime(int.Parse(parts[1]), int.Parse(parts[0]), 1).AddMonths(1).AddDays(-1);
            }

            return new DateTime(int.Parse(parts[2]), int.Parse(parts[1]), int.Parse(parts[0]));
        }
    }
}
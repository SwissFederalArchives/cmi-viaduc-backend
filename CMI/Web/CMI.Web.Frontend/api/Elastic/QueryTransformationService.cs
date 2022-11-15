using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CMI.Web.Frontend.api.Configuration;
using Nest;

namespace CMI.Web.Frontend.api.Elastic
{
    public class QueryTransformationService
    {
        private readonly SearchSetting searchSettings;

        public QueryTransformationService(SearchSetting searchSettings)
        {
            this.searchSettings = searchSettings;
        }

        /// <summary>
        /// Transforms an existing Query that may contain anonynized fields to a query where the field names are
        /// replaced by the unanonymized field names.
        /// </summary>
        /// <param name="querycontainer"></param>
        /// <returns></returns>
        public QueryContainer TransformQuery(QueryContainer querycontainer)
        {
            var serializer = new QueryContainerJsonConverter();
            var original = serializer.Serialize(querycontainer);

            var newQuery = TransformQueryToProtectedFields(original);
            if (string.Empty == newQuery)
            {
                return null;
            }
            byte[] byteArray = Encoding.UTF8.GetBytes(newQuery);
            var stream = new MemoryStream(byteArray);
            var clone = serializer.Deserialize(stream);

            return clone;
        }

        private string TransformQueryToProtectedFields(string original)
        {
            var hasProtectedFields = false;
            var newQuery = original;
            string queryString = @"query_string.*?}";
            string default_field = @"default_field.*?,";
            string patternAll = @"all_\\.";
            string patternAllMetaData = @"all_Metadata_\\.";
            if (Regex.IsMatch(original, queryString))
            {
                foreach (Match searchText in Regex.Matches(original, queryString, RegexOptions.IgnoreCase))
                {
                    if (Regex.IsMatch(searchText.Value, default_field))
                    {
                        var defaultFieldText = Regex.Match(searchText.Value, default_field, RegexOptions.IgnoreCase);
                        var fieldName = Regex.Match(defaultFieldText.Value, ":\"(?<field>.*?)\"").Groups["field"].Value;

                        if (searchSettings.AdvancedSearchFields.Any(adsf => adsf.Key.Equals(fieldName)))
                        {
                            switch (fieldName)
                            {
                                case "title":
                                case "withinInfo":
                                    var newFieldName = "unanonymizedFields." + fieldName;
                                    var newSearchQuery = Regex.Replace(defaultFieldText.Value, fieldName, newFieldName);
                                    string newQueryString = Regex.Replace(searchText.Value, default_field, newSearchQuery);
                                    newQuery = newQuery.Replace(searchText.Value, newQueryString);
                                    hasProtectedFields = true;
                                    break;
                                case "customFields.verwandteVE":
                                case "customFields.bemerkungZurVe":
                                case "customFields.zusatzkomponenteZac1":
                                    newFieldName = "unanonymizedFields." + fieldName.Split('.')[1];
                                    newSearchQuery = Regex.Replace(defaultFieldText.Value, fieldName, newFieldName);
                                    newQueryString = Regex.Replace(searchText.Value, default_field, newSearchQuery);
                                    newQuery = newQuery.Replace(searchText.Value, newQueryString);
                                    hasProtectedFields = true;
                                    break;
                                // Only fields of which protected fields exist must be in the query
                                default:
                                    newQuery = newQuery.Contains(",{\"" + searchText.Value + "}") ? newQuery.Replace(",{\"" + searchText.Value + "}", "") : newQuery.Replace("{\"" + searchText.Value + "},", "");
                                    break;
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"FieldName: {fieldName} is not in the advanced search list");
                        }
                    }
                    else if (Regex.IsMatch(searchText.Value, patternAllMetaData))
                    {
                        newQuery = Regex.Replace(newQuery, patternAllMetaData, @"protected_Metadata_Text\\");
                        hasProtectedFields = true;
                    }
                    else if (Regex.IsMatch(searchText.Value, patternAll))
                    {
                        newQuery = Regex.Replace(newQuery, patternAll, @"protected_Metadata_Text\\");
                        hasProtectedFields = true;
                    }
                }
            }

            return hasProtectedFields ? newQuery : string.Empty;
        }
    }

}
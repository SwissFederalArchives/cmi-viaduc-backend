using CMI.Web.Frontend.api.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Clients.Elasticsearch.QueryDsl;

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
        /// <param name="orginalJson"></param>
        /// <returns></returns>
        public string TransformQuery( string  orginalJson)
        {
            if (string.IsNullOrEmpty(orginalJson))
                return null;
            
            var transformedJson = TransformQueryToProtectedFields(orginalJson);

            if (string.IsNullOrEmpty(transformedJson))
                return orginalJson;
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(transformedJson));

            return transformedJson;
        }

        public Query TransformQuery(Query querycontainer)
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
                        // DecodeUnicodeEscapes Umlaut wiederherstellen
                        fieldName = Regex.Replace(
                            fieldName,
                            @"\\u([0-9A-Fa-f]{4})",
                            match => ((char) Convert.ToInt32(match.Groups[1].Value, 16)).ToString()
                        );
                        if (searchSettings.AdvancedSearchFields.Any(adsf => adsf.Key.Equals(fieldName)) || fieldName.Equals("externalKeys.key") || fieldName.Equals("externalKeys.value"))
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
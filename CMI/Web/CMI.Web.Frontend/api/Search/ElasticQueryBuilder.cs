using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch;

namespace CMI.Web.Frontend.api.Search
{
    public static class ElasticQueryBuilder
    {
        private static Dictionary<string, SearchFieldDefinition> searchFieldDefinitionsByKey;
        internal static Dictionary<string, IFieldTranslator> translatorsByKey;

        public static ElasticQuery BuildElasticQuery(SearchParameters searchFor, UserAccess access)
        {
            if (searchFieldDefinitionsByKey == null)
            {
                InitDictionaries();
            }

            var query = new ElasticQuery
            {
                SearchParameters = searchFor,
                Query = CreateQueryForSearchModel(searchFor.Query, access)
            };

            return query;
        }

        public static Query CreateQueryForSearchModel(SearchModel model, UserAccess access)
        {
            var boolQuery = new BoolQuery();

            Func<SearchGroup, Query> createForGroup = group => CreateQueryForGroup(group, access);
            var groupQueries = model.SearchGroups.Where(g => g.SearchFields.Any()).Select(createForGroup).ToList();

            switch (model.GroupOperator)
            {
                case GroupOperator.And:
                    boolQuery.Must = groupQueries;
                    break;
                case GroupOperator.Or:
                    boolQuery.Should = groupQueries;
                    boolQuery.MinimumShouldMatch = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return boolQuery;
        }

        private static Query CreateForField(SearchField field, UserAccess access)
        {
            if (!translatorsByKey.ContainsKey(field.Key))
            {
                throw new UnknownElasticSearchFieldException(field.Key);
            }

            var translator = translatorsByKey[field.Key];
            return translator.CreateQueryForField(field, access);
        }

        public static Query CreateQueryForGroup(SearchGroup group, UserAccess access)
        {
            var boolQuery = new BoolQuery();
            var fieldQueries = group.SearchFields.Where(f => !string.IsNullOrEmpty(f.Value)).Select(fld => CreateForField(fld, access)).ToList();

            switch (group.FieldOperator)
            {
                case FieldOperator.And:
                    boolQuery.Must = fieldQueries;
                    break;
                case FieldOperator.Or:
                    boolQuery.Should = fieldQueries;
                    boolQuery.MinimumShouldMatch = 1;
                    break;
                case FieldOperator.Not:
                    boolQuery.MustNot = fieldQueries;
                    break;
            }

            return boolQuery;
        }

        public static Query CreateQueryForSignatur(string signatur)
        {
            var boolQuery = new BoolQuery
            {
                Must = new List<Query>
                {
                    new TermsQuery(new Field("referenceCode"), new TermsQueryField(new List<FieldValue>{signatur}))
                }
            };
            return boolQuery;
        }

        public static void InitDictionaries()
        {
            var settings = FrontendSettingsViaduc.Instance.GetServerSettings().DeepClone() as JObject;
            var searchSettings = SettingsHelper.GetSettingsFor<SearchSetting>(settings, "search");

            searchFieldDefinitionsByKey = searchSettings.AdvancedSearchFields.ToDictionary(s => s.Key);
            translatorsByKey = new Dictionary<string, IFieldTranslator>();

            foreach (var s in searchFieldDefinitionsByKey)
            {
                var typeString = s.Value.TranslatorType;
                var type = Assembly.GetExecutingAssembly().GetExportedTypes().First(t =>
                    t.FullName != null && t.FullName.EndsWith($".{typeString}") && t.ImplementsType(typeof(IFieldTranslator)));

                var instance = (IFieldTranslator) Activator.CreateInstance(type);
                translatorsByKey[s.Value.Key] = instance;
            }
        }
    }
}
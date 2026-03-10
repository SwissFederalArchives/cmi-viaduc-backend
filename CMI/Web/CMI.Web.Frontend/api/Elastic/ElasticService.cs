using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Utilities.ActaPro;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.api.Templates;
using Elasticsearch.Net;
using Namotion.Reflection;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using SourceFilter = Nest.SourceFilter;

namespace CMI.Web.Frontend.api.Elastic
{
    public class ElasticService : IElasticService
    {
        // Official Elastic "take" Limit, when skip = 0 - change with care!
        // ReSharper disable once InconsistentNaming
        public const int ELASTIC_SEARCH_HIT_LIMIT = 10000;

        private readonly IElasticClientProvider clientProvider;
        private readonly ISearchRequestBuilder searchRequestBuilder;
        private readonly IElasticSettings elasticSettings;
        private readonly List<TemplateField> internalFields;
        public ActaProMappingProvider ActaProMappingProvider { get; }

        public ElasticService(IElasticClientProvider clientProvider, ISearchRequestBuilder searchRequestBuilder, IElasticSettings elasticSettings,
            List<TemplateField> internalFields)
        {
            this.clientProvider = clientProvider;
            this.searchRequestBuilder = searchRequestBuilder;
            this.elasticSettings = elasticSettings;
            this.internalFields = internalFields;
            var mappingTableDirectory = DirectoryHelper.Instance.MappingTableDirectory != null
                ? DirectoryHelper.Instance.MappingTableDirectory
                // For testing purposes, use the default mapping table directory
                : "Test";
            ActaProMappingProvider = new ActaProMappingProvider(mappingTableDirectory);
        }

        protected string BaseUrl => elasticSettings.BaseUrl;

        public ElasticQueryResult<T> QueryForId<T>(string id, UserAccess access, bool translated = true) where T : TreeRecord
        {
            var query = new ElasticQuery
            {
                SearchParameters = new SearchParameters
                {
                    Options = new SearchOptions
                    {
                        EnableExplanations = false,
                        EnableHighlighting = false,
                        EnableAggregations = false
                    }
                }
            }; 
            query.Query = QueryByIdOrExternalKey(id);

            var result = RunQuery<T>(query, access, translated);
            if (result.Response.Hits.Count == 1)
            {
                return result;
            }

            if (result.Response.Hits.Count == 2)
            {
                // This can happen if we have a record that was not correctly deleted after a successful sync
                // Thus there is still existing the record with the scopeId as the primary key and the
                // new record with the doc key. THIS SHOULD NOT HAPPEN, BUT when it does, return the docKey and delete the scopeId
                RemoveRecordWithScopeId(result);

                // Call again the same. Should now return 1 record
                return QueryForId<T>(id, access, translated);
            }

            if (result.Response.Hits.Count > 2)
            {
                throw new ArgumentException($"Query For Id must return exactly one record, but we have found {result.Response.Hits.Count} for id {id}");
            }

            return null;
        }

        /// <summary>
        /// Die Methode liefert eine Liste mit allen Kindern der übergebenen Vater-Id unter Berücksichtigung der Benutzerberechtigung.
        /// </summary>
        /// <remarks>
        /// Wegen PVW-1178 (Timeout beim Öffnen von grossen anonymisierten Serien in BAR-Rolle) wurde die Methode dahingehend optimiert,
        /// dass die Query Resultate vom Typ <see cref="ElasticArchiveDbRecord"/> anfordert. Dabei werden aber über die SourceFilter
        /// nur diejenigen Eingenschaften zurückgeliefert, die es für TreeRecords benötigt, plus die nicht anonymisierten Felder.
        /// Dies damit es für das sichtbarmachen der Daten für BAR Benutzer nicht erneut zusätzliche Elastic Abfragen braucht.
        /// </remarks>
        /// <param name="id">Die ArchiveRecord ID der VE dessen Kinder geholt werden sollen.</param>
        /// <param name="access">Die Zugriffsrechte des Benutzers</param>
        /// <returns></returns>
        public List<TreeRecord> QueryForParentId(string id, UserAccess access)
        {
            SearchScopeOrActaProId(id, out long scopeId, out string actaProId);

            return QueryForParentId(access, scopeId, actaProId);
        }

        public ElasticQueryResult<T> QueryForIds<T>(IList<string> ids, UserAccess access, Paging p = null) where T : TreeRecord
        {
            var query = BuildQueryForIds(ids, p);
            return RunQuery<T>(query, access);
        }

        public ElasticQueryResult<T> QueryForIdsWithoutSecurityFilter<T>(IList<string> ids, Paging p = null) where T : TreeRecord
        {
            var query = BuildQueryForIds(ids, p);
            return RunQueryWithoutSecurityFilters<T>(query);
        }

        public ElasticQueryResult<T> RunQuery<T>(ElasticQuery query, UserAccess access, bool translated = true) where T : TreeRecord
        {
            var stopwatch = new Stopwatch();
            var info = StringHelper.AddToString(BaseUrl, "/", elasticSettings.DefaultIndex);
            if (!string.IsNullOrEmpty(elasticSettings.DefaultTypeName))
            {
                info += "/" + elasticSettings.DefaultTypeName;
            }

            var result = new ElasticQueryResult<T>
            {
                RequestInfo = info,
                Status = 0,
                TimeInMilliseconds = 0,
                EnableExplanations = query.SearchParameters?.Options?.EnableExplanations ?? false
            };

            var client = clientProvider.GetElasticClient(elasticSettings, result);

            try
            {
                stopwatch.Start();
                var searchRequest = searchRequestBuilder.Build(query, access);
                result.Response = client.Search<T>(searchRequest);

                var json = client.RequestResponseSerializer.SerializeToString(searchRequest, SerializationFormatting.Indented);
                Log.Debug(json);

                stopwatch.Stop();
                result.TimeInMilliseconds = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine($"Fetched record from web in  {stopwatch.ElapsedMilliseconds}ms");
                result.Status = (int) HttpStatusCode.OK;

                ProcessQueryResult(result, query.SearchParameters?.FacetsFilters, access, client.SourceSerializer, translated);
            }
            catch (Exception ex)
            {
                var statusCode = (ex as ElasticsearchClientException)?.Response?.HttpStatusCode;

                if (statusCode.HasValue)
                {
                    Log.Warning(ex, "Exception on Elastic query: {0}", result.RequestInfo);
                    result.Status = statusCode.Value;
                }
                else
                {
                    Log.Error(ex, "Exception on Elastic query: {0}", result.RequestInfo);
                    result.Status = (int) HttpStatusCode.InternalServerError;
                }

                var debugInformation = (ex as ElasticsearchClientException)?.DebugInformation;

                if (!string.IsNullOrEmpty(debugInformation))
                {
                    Log.Information(ex, "Additional information about the prior exception. Debug information: {0}",
                        debugInformation);
                }

                result.Exception = ex;
            }
            finally
            {
                Log.Debug("RunQueryCompleted: {RequestInfo}, {RequestRaw}, {ResponseRaw}", result.RequestInfo, result.RequestRaw, result.ResponseRaw);
            }

            return result;
        }

        public string[] GetLaender()
        {
            var searchRequest = new SearchRequest<ElasticArchiveRecord>
            {
                Aggregations = new AggregationDictionary
                {
                    {
                        "A", new TermsAggregation("B")
                        {
                            Field = "customFields.land.keyword", Size = int.MaxValue
                        }
                    }
                },
                Size = 0
            };

            var client = clientProvider.GetElasticClient<TreeRecord>(elasticSettings);
            var response = client.Search<TreeRecord>(searchRequest);

            return ((BucketAggregate) response.Aggregations["A"]).Items
                .Select(keyedBucket => ((KeyedBucket<object>) keyedBucket).Key.ToString())
                .OrderBy(value => value)
                .ToArray();
        }

        public ElasticQueryResult<T> RunQueryWithoutSecurityFilters<T>(ElasticQuery query) where T : TreeRecord
        {
            var info = StringHelper.AddToString(BaseUrl, "/", elasticSettings.DefaultIndex);
            info = StringHelper.AddToString(info, "/", elasticSettings.DefaultTypeName);
            var result = new ElasticQueryResult<T>
            {
                RequestInfo = info,
                Status = 0,
                TimeInMilliseconds = 0
            };

            var client = clientProvider.GetElasticClient(elasticSettings, result);

            try
            {
                var started = DateTime.Now;

                var request = new SearchRequest<ElasticArchiveRecord> {Query = query.Query};
                var p = query.SearchParameters;

                searchRequestBuilder.AddPaging(p?.Paging, request);
                searchRequestBuilder.AddSort(p?.Paging?.OrderBy, p?.Paging?.SortOrder, request);
                searchRequestBuilder.ExcludeUnwantedFields(request);

                result.Response = client.Search<T>(request);
                result.TimeInMilliseconds = (int) Math.Round((DateTime.Now - started).TotalMilliseconds);
                result.Status = (int) HttpStatusCode.OK;
                // Ohne Übersetzung, weil es nur Daten für BAR Benutzer speichert/anzeigt
                ProcessQueryResult(result, null, null, client.SourceSerializer, false);
            }
            catch (Exception ex)
            {
                var statusCode = (ex as ElasticsearchClientException)?.Response?.HttpStatusCode;

                if (statusCode.HasValue)
                {
                    Log.Warning(ex, "Exception on Elastic query: {0}", result.RequestInfo);
                    result.Status = statusCode.Value;
                }
                else
                {
                    Log.Error(ex, "Exception on Elastic query: {0}", result.RequestInfo);
                    result.Status = (int) HttpStatusCode.InternalServerError;
                }

                var debugInformation = (ex as ElasticsearchClientException)?.DebugInformation;

                if (!string.IsNullOrEmpty(debugInformation))
                {
                    Log.Information(ex, "Additional information about the prior exception. Debug information: {0}", debugInformation);
                }

                result.Exception = ex;
            }

            return result;
        }


        public ElasticQueryResult<T> QueryForRootNodes<T>(UserAccess access) where T : TreeRecord
        {
            var query = new ElasticQuery
            {
                SearchParameters = new SearchParameters
                {
                    Options = new SearchOptions
                    {
                        EnableExplanations = false,
                        EnableHighlighting = false,
                        EnableAggregations = false
                    }
                },
                Query = new MatchQuery
                {
                    Field = elasticSettings.TreeLevelField,
                    Query = "1"
                }
            };

            return RunQuery<T>(query, access);
        }

        private List<TreeRecord> QueryForParentId(UserAccess access, long scopeId, string actaProId)
        {
            var client = clientProvider.GetElasticClient<TreeRecord>(elasticSettings);
            var result = new List<TreeRecord>();
            // Den SourceFilter erstellen, der nur die Felder des TreeRecords enthält
            // Danach noch um die unanonymisierten Felder erweitern.
            var sourceFilter = GetSourceFilterForType<TreeRecord>();
            sourceFilter.Includes.And(Infer.Field("UnanonymizedFields".ToLowerCamelCase()));

            var search = new SearchDescriptor<ElasticArchiveDbRecord>()
                .Index(elasticSettings.DefaultIndex)
                .From(0)
                .Sort(s => s.Ascending(nameof(TreeRecord.Title).ToLowerCamelCase()))
                .Sort(s => s.Ascending(nameof(TreeRecord.TreeSequence).ToLowerCamelCase()))
                .Query(_ =>
                    searchRequestBuilder.GetQueryWithSecurity(CreateQueryForParentScopeId(scopeId, actaProId), access)
                )
                .Size(10000)
                .Source(x => x.Includes(i => i.Fields(sourceFilter.Includes)))
                .Scroll("15s");

            // Suche ausführen
            var resultPart = client.Search<ElasticArchiveDbRecord>(search);

            while (resultPart.IsValid && resultPart.Documents.Count > 0)
            {
                // Die unanonymisierten Daten für die berechtigten Benutzer "aufdecken"
                foreach (var treeRecord in resultPart.Documents.Where(d => d.IsAnonymized))
                {
                    if (access != null && access.HasAnyTokenFor(treeRecord.FieldAccessTokens))
                    {
                        treeRecord.SetUnanonymizedValuesForAuthorizedUser(treeRecord);
                    }
                }
                result.AddRange(resultPart.Documents.Select(d => (TreeRecord) d));
                resultPart = client.Scroll<ElasticArchiveDbRecord>("15s", resultPart.ScrollId);
            }
            return result;
        }


        private ElasticArchiveDbRecord GetElasticDbRecordById(string archiveRecordId, UserAccess access)
        {
            var dbRecord = QueryForId<ElasticArchiveDbRecord>(archiveRecordId, access);

            if (dbRecord.Response.Hits.Count == 1)
            {
                return dbRecord.Response.Hits.First().Source;
            }

            return null;
        }

        private BoolQuery CreateQueryForParentScopeId(long scopeId, string actaProId)
        {
            var boolQuery = new BoolQuery();
            if (string.IsNullOrEmpty(actaProId) && scopeId < 1)
            {
                return boolQuery;
            }
            if (scopeId > 0)
            {
                boolQuery.Should = new QueryContainer[]
                {
                    new TermQuery
                    {
                        Field = nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase(),
                        Value = scopeId
                    }, 
                    new TermQuery
                    {
                        Field = nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase(),
                        Value = actaProId
                    }
                };
            }
            else
            {
                boolQuery.Must = new QueryContainer[]
                {
                    new TermQuery
                    {
                        Field = nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase(),
                        Value = actaProId
                    }
                };
            }

            return boolQuery;
        }


        private ElasticQuery BuildQueryForIds(IList<string> ids, Paging p)
        {
            var query = new ElasticQuery
            {
                SearchParameters = new SearchParameters
                {
                    Paging = p,
                    Options = new SearchOptions
                    {
                        EnableExplanations = false,
                        EnableHighlighting = false,
                        EnableAggregations = false
                    }
                }
            };
            if (ids.Count != 0)
            {
                var boolQuery = new BoolQuery();
                var idQueries = new List<QueryContainer>();
                foreach (var id in ids)
                {
                    idQueries.Add(QueryByIdOrExternalKey(id));
                }

                boolQuery.Should = idQueries;
                query.Query = boolQuery;
            }
            else
            {
                query.Query = new MatchNoneQuery();
            }

            return query;
        }

        private BoolQuery QueryByIdOrExternalKey(string id)
        {
            var externalKeyOrId = new BoolQuery();

            if (int.TryParse(id, out int _))
            {
                var queryForId = new TermQuery
                {
                    Field = elasticSettings.IdField,
                    Value = id
                };
                var queryForExternalKeys = new ExternalKeysQueryProvider();
                var externalScopeKeysQuery = queryForExternalKeys.CreateExternalKeysQuery(id, "scopeArchiv");
                var docKey = ActaProMappingProvider.GetActaProId(id);
                if (!string.IsNullOrEmpty(docKey))
                {
                    var externalKeysDocKeyQuery = queryForExternalKeys.CreateExternalKeysQuery(docKey, "ActaPro");
                    externalKeyOrId.Should = new[] {externalScopeKeysQuery, externalKeysDocKeyQuery, queryForId};
                }
                else
                {
                    externalKeyOrId.Should = new[] { externalScopeKeysQuery, queryForId };
                }
            }
            else
            {
                var queryForId = new TermQuery
                {
                    Field = elasticSettings.IdField,
                    Value = id
                };
                var queryForExternalKeys = new ExternalKeysQueryProvider();
                var externalKeysDocKeyQuery = queryForExternalKeys.CreateExternalKeysQuery(id, "ActaPro");
                var mappedScopeId = ActaProMappingProvider.GetScopeId(id);
                if (mappedScopeId > 0)
                {
                    var externalScopeKeysQuery = queryForExternalKeys.CreateExternalKeysQuery(mappedScopeId.ToString(), "scopeArchiv");
                    externalKeyOrId.Should = new[] { externalScopeKeysQuery, externalKeysDocKeyQuery, queryForId };
                }
                else
                {
                    externalKeyOrId.Should = new[] { externalKeysDocKeyQuery, queryForId };
                }
            }

            return externalKeyOrId;
        }

        private void ProcessQueryResult<T>(ElasticQueryResult<T> result, FacetFilters[] facetsFilters, UserAccess access,
            IElasticsearchSerializer serializer, bool translated = true) where T : TreeRecord
        {
            var response = result.Response;

            var hits = response?.Hits ?? new List<IHit<T>>();

            result.TotalNumberOfHits = response?.HitsMetadata != null ? (int) response.HitsMetadata.Total.Value : -1;
            var entries = new List<Entity<T>>();
            foreach (var hit in hits)
            {
                var data = JsonConvert.DeserializeObject<T>(serializer.SerializeToString(hit.Source));

                ProcessAnonymizedRecords(data, access);

                var entry = new Entity<T>
                {
                    Data = data,
                    Highlight = hit.GetHighlightingObj(access, data.Title),
                    Explanation = hit.GetExplanationObj(serializer)
                };

                if (access != null)
                {
                    if (translated)
                    {
                        data.Translate(access.Language);
                    }
                    entry.IsDownloadAllowed = access.HasAnyTokenFor(data?.PrimaryDataDownloadAccessTokens);
                }

                // Remove internal fields
                // This is only for added security, as internal fields are actually excluded from the result set
                if (access == null || !access.HasAnyTokenFor(new[] {AccessRolesEnum.BAR.ToString()}))
                {
                    RemoveInternalFields(data);
                }

                entries.Add(entry);
            }

            var entityResult = new EntityResult<T> {Items = entries};

            if (response?.Aggregations.Any() != null)
            {
                var filteredAggregations = GetfilteredAggregations(response.Aggregations, facetsFilters, out var chosenCreationPeriodAggregation);
                var facette = filteredAggregations.CreateSerializableAggregations();

                ComplementAggregations(facette, chosenCreationPeriodAggregation);
                result.Facets = facette;
            }

            result.Data = entityResult;
        }

        private void ProcessAnonymizedRecords<T>(T data, UserAccess access) where T : TreeRecord
        {
            if (data.IsAnonymized && access != null && access.HasAnyTokenFor(data.FieldAccessTokens))
            {
                var dbRecord = data as ElasticArchiveDbRecord ?? GetElasticDbRecordById(data.ArchiveRecordId, access);

                if (dbRecord != null)
                {
                    data.SetUnanonymizedValuesForAuthorizedUser(dbRecord);
                }
            }
        }

        /// <summary>
        ///     Die Methode für die folgenden Filterungen durch:
        ///     - Es wird nur eine creationPeriod Facette zurückgegeben
        ///     - Aggregationen die mit 'facet_' beginnen werden nicht zurückgegeben. Stattdessen wird das Child von diesen
        ///     Aggregationen zurückgegeben.
        /// </summary>
        private Dictionary<string, IAggregate> GetfilteredAggregations(AggregateDictionary aggs, FacetFilters[] facetsFilters,
            out string chosenCreationPeriodAggregation)
        {
            var filteredAggregations = new Dictionary<string, IAggregate>();
            var found = false;
            chosenCreationPeriodAggregation = string.Empty;

            foreach (var entry in aggs.OrderBy(t => t.Key))
            {
                if (entry.Key.StartsWith("facet_aggregationFields.creationPeriodYears"))
                {
                    if (!found)
                    {
                        var primaryAggregation = ((SingleBucketAggregate) entry.Value).First().Value;

                        // Wähle den Bucket, der weniger als 10 Einträge hat. Oder dann ganz am Ende den Jahrhundertfilter
                        if (GetSelectedCreationPeriod(facetsFilters) == string.Empty && (((BucketAggregate) primaryAggregation).Items.Count < 10 ||
                                                                                         entry.Key == "facet_aggregationFields.creationPeriodYears100"
                            ) ||
                            GetSelectedCreationPeriod(facetsFilters) == entry.Key)
                        {
                            filteredAggregations.Add("aggregationFields.creationPeriodYears", primaryAggregation);
                            found = true;
                            chosenCreationPeriodAggregation = entry.Key.Remove(0, 6);
                        }
                    }
                }
                else if (entry.Key.StartsWith("facet_aggregationFields.protectionEndDateDossier"))
                {
                    var primaryAggregation = ((SingleBucketAggregate) entry.Value).First().Value;
                    filteredAggregations.Add("aggregationFields.protectionEndDateDossier", primaryAggregation);
                }
                else if (entry.Key.StartsWith("facet_"))
                {
                    var primaryAggregation = ((SingleBucketAggregate) entry.Value).First().Value;
                    filteredAggregations.Add(entry.Key.Remove(0, 6), primaryAggregation);
                }
                else
                {
                    filteredAggregations.Add(entry.Key, entry.Value);
                }
            }

            return filteredAggregations;
        }

        private void ComplementAggregations(JObject aggregations, string chosenCreationPeriodAggregation)
        {
            foreach (var aggregation in aggregations.Children())
            {
                var aggregationName = ((JProperty) aggregation).Name;
                var itemCollection = aggregation.Children()["items"].Children();

                if (aggregationName == "aggregationFields.creationPeriodYears")
                {
                    var itemRange = Convert.ToInt32(chosenCreationPeriodAggregation.Substring(chosenCreationPeriodAggregation.Length - 3));

                    foreach (var item in itemCollection)
                    {
                        var begin = item["key"].Value<int>();
                        var end = begin + itemRange - 1;
                        string newKey;
                        string filter;

                        if (begin != 0)
                        {
                            newKey = itemRange != 1 ? $"{begin} - {end}" : $"{begin}";
                            filter = $"{aggregationName}{itemRange:D3}:\"{item["key"]}\"";
                        }
                        else
                        {
                            newKey = "search.facetteEntry.resultateOhneZeitraum";
                            filter = $"(!_exists_:{aggregationName}{itemRange:D3})";
                        }

                        item["key"] = newKey;
                        item["filter"] = filter;
                    }
                }
                else if (aggregationName == "aggregationFields.protectionEndDateDossier")
                {
                    foreach (var item in itemCollection)
                    {
                        var begin = item["key"].Value<int>(); ;
                        item["key"] = $"{begin}";
                        item["filter"] = $"{aggregationName}.year:\"{begin}\"";
                    }
                }
                else
                {
                    foreach (var item in itemCollection)
                    {
                        var key = string.IsNullOrWhiteSpace(item["keyAsString"]?.ToString())
                            ? item["key"]?.ToString()
                            : item["keyAsString"].ToString();
                        
                        if(aggregationName == "customFields.zugänglichkeitGemässBga" || aggregationName == "level")
                        {
                            item["key"] = "search.facetteEntry." + key;
                        }
                        else if (aggregationName == "aggregationFields.ordnungskomponenten")
                        {
                            item["key"] = "search.facetteEntry.thematicInventoryOverview." + key;
                        }

                        item["filter"] = $"{aggregationName}:\"{key}\"";
                    }
                }
            }
        }

        private static string GetSelectedCreationPeriod(FacetFilters[] facetsFilters)
        {
            var creationPeriod = facetsFilters?.FirstOrDefault(item => item.Facet == "aggregationFields.creationPeriodYears");

            if (creationPeriod?.Filters == null)
            {
                return string.Empty;
            }

            foreach (var filter in creationPeriod.Filters)
            {
                var aggregation = filter.Substring(0, filter.IndexOf(':'));
                if (aggregation.StartsWith("aggregationFields.creationPeriodYears", StringComparison.InvariantCultureIgnoreCase))
                {
                    return $"facet_{aggregation}";
                }
            }

            return string.Empty;
        }
        
        private void Postprocess(JObject entity, bool omitEmptyValues = true)
        {
            if (entity == null)
            {
                return;
            }

            var adds = new List<JToken>();
            var removes = new List<JToken>();
            var attributes = new JObject();

            foreach (var jSub in entity.Children())
            {
                var prop = jSub as JProperty;
                if (prop == null)
                {
                    continue;
                }

                var value = prop.Value;
                var vType = value != null ? value.Type : JTokenType.Null;
                if (vType == JTokenType.None || vType == JTokenType.Null)
                {
                    if (omitEmptyValues)
                    {
                        removes.Add(jSub);
                    }
                }
                else if (!Postprocess(prop.Value as JArray, omitEmptyValues))
                {
                    Postprocess(prop.Value as JObject, omitEmptyValues);
                }
            }

            // remove superfluous tokens
            foreach (var remove in removes)
            {
                if (remove.Parent != null)
                {
                    remove.Remove();
                }
            }

            // add all in adds as first in reverse order ...
            adds.Reverse();
            foreach (var add in adds)
            {
                entity.AddFirst(add);
            }

            // add attributes ...
            if (attributes.Count <= 0)
            {
                return;
            }

            // promote properties in attributes without name conflict to entity
            var promoted = new List<JProperty>();
            foreach (var attribute in attributes.Children().OfType<JProperty>())
            {
                if (entity.SelectToken(attribute.Name) != null)
                {
                    continue;
                }

                entity.Add(attribute);
                promoted.Add(attribute);
            }

            // finally, add all remaining attributes as "_attributes" object 
            foreach (var property in promoted)
            {
                attributes.Remove(property.Name);
            }

            if (attributes.Count > 0)
            {
                entity.AddFirst(new JProperty("_attributes", attributes));
            }
        }

        private bool Postprocess(JArray elements, bool omitEmptyValues = true)
        {
            if (elements == null)
            {
                return false;
            }

            foreach (var element in elements.Children())
            {
                if (!Postprocess(element as JArray, omitEmptyValues))
                {
                    Postprocess(element as JObject, omitEmptyValues);
                }
            }

            return true;
        }

        private void RemoveInternalFields<T>(T data) where T : TreeRecord
        {
            if (data is SearchRecord searchRecord)
            {
                foreach (var internalField in internalFields)
                {
                    var isCustomField = internalField.DbFieldName.StartsWith("CustomFields");
                    var fieldName = isCustomField ? internalField.DbFieldName.Split('.')[1].ToLowerCamelCase() : internalField.DbFieldName;
                    if (isCustomField ? searchRecord.HasCustomProperty(fieldName) : searchRecord.HasProperty(fieldName))
                    {
                        if (!isCustomField)
                        {
                            var prop = searchRecord.GetType().GetProperty(fieldName);
                            if (prop != null)
                            {
                                prop.SetValue(searchRecord, null);
                            }
                        }
                        else
                        {
                            var keyValues = (IDictionary<string, object>) searchRecord.CustomFields;
                            Debug.WriteLine($"Removed field {fieldName} from customfields");
                            keyValues[fieldName] = null;
                        }
                    }
                }
            }
        }

        private SourceFilter GetSourceFilterForType<T>() where T : class
        {
            var fields = typeof(T).GetProperties().Select(p => p.Name.ToLowerCamelCase()).ToArray();
            var filter = new SourceFilter() { Includes = Infer.Fields(fields) };
            return filter;
        }

        public AccessTokens QueryTokensForId(string archiveRecordId)
        {
            var query = new ElasticQuery
            {
                SearchParameters = new SearchParameters
                {
                    Options = new SearchOptions
                    {
                        EnableExplanations = false,
                        EnableHighlighting = false,
                        EnableAggregations = false
                    }
                },
                Query = new TermQuery
                {
                    Field = elasticSettings.IdField,
                    Value = archiveRecordId
                }
            };

            var result = RunQueryWithoutSecurityFilters<ElasticArchiveRecord>(query);
            var record = result?.Response?.Hits?.FirstOrDefault()?.Source;

            if (record == null)
                return new AccessTokens(); // empty result instead of null for safety

            return new AccessTokens
            {
                MetadataAccessTokens = string.Join(", ", record.MetadataAccessTokens ?? new List<string>()),
                FulltextAccessTokens = string.Join(", ", record.PrimaryDataFulltextAccessTokens ?? new List<string>()),
                DownloadAccessTokens = string.Join(", ", record.PrimaryDataDownloadAccessTokens ?? new List<string>()),
                FieldAccessTokens = string.Join(", ", record.FieldAccessTokens ?? new List<string>())
            };
        }

        private void SearchScopeOrActaProId(string id, out long scopeId, out string actaProId)
        {
            if (long.TryParse(id, out scopeId))
            {
                actaProId = ActaProMappingProvider.GetActaProId(id);
            }
            else
            {
                actaProId = id;
                scopeId = ActaProMappingProvider.GetScopeId(id);
            }
        }

        /// <summary>
        /// Removes one of the records from the elastic index, if the query result contains two hits 
        /// when queried by id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        private void RemoveRecordWithScopeId<T>(ElasticQueryResult<T> result) where T : TreeRecord
        {
            // Make sure we only process data where we have exactly two hits.
            if (result.Response.Hits.Count != 2)
            {
                return;
            }

            var client = clientProvider.GetElasticClient<T>(elasticSettings);
            var hit1 = result.Response.Hits.First();
            var hit2 = result.Response.Hits.Last();

            if (long.TryParse(hit1.Id, out var hit1Id))
            {
                // Validation: Make sure the id's of the two hits are equal
                var docKey = ActaProMappingProvider.GetActaProId(hit1Id.ToString());
                if (docKey.Equals(hit2.Id, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Delete
                    Log.Information("Deleting elastic record with id {hit1Id} after query by id returned two hits", hit1Id);
                    client.Delete<ElasticArchiveRecord>(hit1Id);
                    return;
                }
            }

            if (long.TryParse(hit2.Id, out var hit2Id))
            {
                // Validation: Make sure the id's of the two hits are equal
                var docKey = ActaProMappingProvider.GetActaProId(hit2Id.ToString());
                if (docKey.Equals(hit1.Id, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Delete
                    Log.Information("Deleting elastic record with id {hit2Id} after query by id returned two hits", hit2Id);
                    client.Delete<ElasticArchiveRecord>(hit2Id);
                    return;
                }
            }

            // If we come here, we have the strange case, that we have identical docKeys, but with different length.
            // When testing we have found: 
            // Vz    d592a897-aa0d-5e1c-9cbb-a18f35491dba
            // Vz      d592a897-aa0d-5e1c-9cbb-a18f35491dba
            // The correct length is 44 chars
            var guidPart1 = hit1.Id.Split(' ').Select(t => t.Trim()).Last();
            var guidPart2 = hit2.Id.Split(' ').Select(t => t.Trim()).Last();
            if (guidPart1 == guidPart2)
            {
                // hit 2 is not correct
                if (hit1.Id.Length == 44 && hit2.Id.Length != 44)
                {
                    Log.Information("Deleting elastic record with id {hit2Id} after query by id returned two hits", hit2.Id);
                    client.Delete<ElasticArchiveRecord>(hit2.Id);
                }

                // hit 1 is not correct
                if (hit2.Id.Length == 44 && hit1.Id.Length != 44)
                {
                    Log.Information("Deleting elastic record with id {hit1Id} after query by id returned two hits", hit1.Id);
                    client.Delete<ElasticArchiveRecord>(hit1.Id);
                }
            }
        }


    }
}
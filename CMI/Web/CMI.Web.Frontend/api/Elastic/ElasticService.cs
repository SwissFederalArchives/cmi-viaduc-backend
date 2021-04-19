using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Elasticsearch.Net;
using Microsoft.Ajax.Utilities;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SourceFilter = Nest.SourceFilter;

namespace CMI.Web.Frontend.api.Elastic
{
    public class ElasticService : IElasticService
    {
        // Official Elastic "take" Limit, when skip = 0 - change with care!
        // ReSharper disable once InconsistentNaming
        public const int ELASTIC_SEARCH_HIT_LIMIT = 10000;

        private static readonly string[] allowedFacetFields =
        {
            "level", "customFields.zugänglichkeitGemässBga", "aggregationFields.ordnungskomponenten", "aggregationFields.bestand",
            "aggregationFields.hasPrimaryData", "aggregationFields.creationPeriodYears001", "aggregationFields.creationPeriodYears005",
            "aggregationFields.creationPeriodYears005", "aggregationFields.creationPeriodYears025", "aggregationFields.creationPeriodYears100"
        };

        private readonly IElasticClientProvider clientProvider;
        private readonly IElasticSettings elasticSettings;

        public ElasticService(IElasticClientProvider clientProvider, IElasticSettings elasticSettings)
        {
            this.clientProvider = clientProvider;
            this.elasticSettings = elasticSettings;
        }

        protected string BaseUrl => elasticSettings.BaseUrl;

        public ElasticQueryResult<T> QueryForId<T>(int id, UserAccess access) where T : TreeRecord
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
                    Value = id.ToStringInvariant()
                }
            };

            return RunQuery<T>(query, access);
        }

        public List<TreeRecord> QueryForParentId(int id, UserAccess access)
        {
            var client = clientProvider.GetElasticClient<TreeRecord>(elasticSettings);
            var result = new List<TreeRecord>();

            var resultPart = client.Search<TreeRecord>(x => x
                .Index(elasticSettings.DefaultIndex)
                .Type(elasticSettings.DefaultTypeName)
                .From(0)
                .Sort(s => s.Ascending(nameof(TreeRecord.Title).ToLowerCamelCase()))
                .Sort(s => s.Ascending(nameof(TreeRecord.TreeSequence).ToLowerCamelCase()))
                .Query(q => GetQueryWithSecurity(new TermQuery
                {
                    Field = elasticSettings.ParentIdField,
                    Value = id.ToStringInvariant()
                }, access))
                .Size(10000)
                .Scroll("15s"));

            while (resultPart.IsValid && resultPart.Documents.Count > 0)
            {
                result.AddRange(resultPart.Documents);
                resultPart = client.Scroll<TreeRecord>("15s", resultPart.ScrollId);
            }

            return result;
        }

        public ElasticQueryResult<T> QueryForIds<T>(IList<int> ids, UserAccess access, Paging p = null) where T : TreeRecord
        {
            var query = BuildQueryForIds(ids, p);
            return RunQuery<T>(query, access);
        }

        public ElasticQueryResult<T> QueryForIdsWithoutSecurityFilter<T>(IList<int> ids, Paging p = null) where T : TreeRecord
        {
            var query = BuildQueryForIds(ids, p);
            return RunQueryWithoutSecurityFilters<T>(query);
        }

        public ElasticQueryResult<T> RunQuery<T>(ElasticQuery query, UserAccess access) where T : TreeRecord
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
                var searchRequest = BuildSearchRequest(query, access);
                result.Response = client.Search<T>(searchRequest);
                stopwatch.Stop();
                result.TimeInMilliseconds = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine($"Fetched record from web in  {stopwatch.ElapsedMilliseconds}ms");
                result.Status = (int) HttpStatusCode.OK;

                ProcessQueryResult(result, query.SearchParameters?.FacetsFilters, access, client.SourceSerializer);
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

                AddPaging(p?.Paging, request);
                AddSort(p?.Paging?.OrderBy, p?.Paging?.SortOrder, request);
                ExcludeUnwantedFields(request);

                result.Response = client.Search<T>(request);
                result.TimeInMilliseconds = (int) Math.Round((DateTime.Now - started).TotalMilliseconds);
                result.Status = (int) HttpStatusCode.OK;

                ProcessQueryResult(result, null, null, client.SourceSerializer);
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


        private SearchRequest<ElasticArchiveRecord> BuildSearchRequest(ElasticQuery query, UserAccess access)
        {
            var request = new SearchRequest<ElasticArchiveRecord>(elasticSettings.DefaultIndex);

            var queryWithSecurity = GetQueryWithSecurity(query.Query, access);

            request.Query = queryWithSecurity;
            request.PostFilter = CreateQuery(query.SearchParameters.FacetsFilters);

            var parameters = query.SearchParameters;
            var options = parameters?.Options;

            AddPaging(parameters?.Paging, request);
            AddSort(parameters?.Paging?.OrderBy, parameters?.Paging?.SortOrder, request);
            ExcludeUnwantedFields(request);

            if (options?.EnableAggregations ?? false)
            {
                AddAggregations(request, parameters?.FacetsFilters);
            }

            if (options?.EnableHighlighting ?? false)
            {
                AddHighlighting(request);
            }

            if (options?.EnableExplanations ?? false)
            {
                request.Explain = true;
            }

            return request;
        }

        private ElasticQuery BuildQueryForIds(IList<int> ids, Paging p)
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
                query.Query = new TermsQuery
                {
                    Field = elasticSettings.IdField,
                    Terms = ids.Select(i => i.ToStringInvariant())
                };
            }
            else
            {
                query.Query = new MatchNoneQuery();
            }

            return query;
        }


        private static BoolQuery GetQueryWithSecurity(QueryContainer querycontainer, UserAccess access)
        {
            if (access == null)
            {
                throw new ArgumentNullException(nameof(access));
            }

            if (access.CombinedTokens?.Length == 0)
            {
                throw new ArgumentException($"{nameof(access)} is not valid");
            }

            var queryWithSecurity = new BoolQuery
            {
                Must = new[] {querycontainer}
            };

            queryWithSecurity.Filter = new QueryContainer[]
            {
                new TermsQuery
                {
                    Field = "metadataAccessTokens",
                    Terms = access.CombinedTokens
                }
            };

            return queryWithSecurity;
        }

        private static void AddPaging(Paging paging, SearchRequest<ElasticArchiveRecord> searchRequest)
        {
            if (paging == null)
            {
                return;
            }

            if (paging.NumberToSkip > 0)
            {
                searchRequest.From = paging.NumberToSkip;
            }

            if (paging.NumberToTake > 0)
            {
                searchRequest.Size = paging.NumberToTake;
            }
        }

        private static void AddSort(string orderBy, string order, SearchRequest<ElasticArchiveRecord> searchRequest)
        {
            searchRequest.Sort = new List<ISort>();

            if (!string.IsNullOrEmpty(orderBy) && !string.IsNullOrEmpty(order))
            {
                SortOrder? sortOrder;
                switch (order.ToLowerInvariant())
                {
                    case "ascending":
                        sortOrder = SortOrder.Ascending;
                        break;

                    case "descending":
                        sortOrder = SortOrder.Descending;
                        break;

                    default:
                        throw new ArgumentException(
                            "The parameter sortOrder contains an invalid value. Valid values: 'ascending', 'descending', empty");
                }

                searchRequest.Sort.Add(new SortField {Field = orderBy, Order = sortOrder});

                // Falls teilweise nach mehreren Feldern sortiert werden soll, könnte eine Ergänzung wie folgt hinzugefügt werden:
                // if (orderBy == "treePath")
                // {
                //    searchRequest.Sort.Add(new SortField { Field = "treeSequence", Order = sortOrder });
                // }
            }

            searchRequest.Sort.Add(new SortField {Field = "_score", Order = SortOrder.Descending});

            // Das folgende Sortierfeld ist ein "tie-breaker", damit die Reihenfolge immer klar definiert ist, auch wenn alle bisherigen Sort-Felder den gleichen Inhalt haben.
            // Die Reihenfolge muss definiert sein, damit das Paging funktioniert, denn wenn sich die Reihenfolge ändert zwischen zwei Seitenaufrufen,
            // wäre ein Paging sinnlos.
            searchRequest.Sort.Add(new SortField {Field = "referenceCode", Order = SortOrder.Ascending});
        }

        private static void AddAggregations(SearchRequest<ElasticArchiveRecord> searchRequest, FacetFilters[] facetsFilters)
        {
            var aggregations = CreateFacet(new TermsAggregation("level") {Field = "level.keyword", Size = int.MaxValue}, facetsFilters);
            aggregations &=
                CreateFacet(new TermsAggregation("customFields.zugänglichkeitGemässBga") {Field = "customFields.zugänglichkeitGemässBga", Size = 25},
                    facetsFilters);
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.ordnungskomponenten") {Field = "aggregationFields.ordnungskomponenten", Size = 25},
                    facetsFilters);
            aggregations &= CreateFacet(new TermsAggregation("aggregationFields.bestand") {Field = "aggregationFields.bestand", Size = 25},
                facetsFilters); // Performance: Limit to 25
            aggregations &=
                CreateFacet(new TermsAggregation("aggregationFields.hasPrimaryData") {Field = "aggregationFields.hasPrimaryData", Missing = "false"},
                    facetsFilters);

            // Zeitraum Filter
            // Für die feinen Filter reicht, wenn wir maximal 10 Stück zurückliefern. Da wir am Ende nur die Facette zurückliefern, die  weniger als 10 Buckets haben
            var order = new List<TermsOrder> {new TermsOrder {Key = "_term"}};
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.creationPeriodYears001")
                        {Field = "aggregationFields.creationPeriodYears001", Size = 10, Missing = "0", Order = order}, facetsFilters);
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.creationPeriodYears005")
                        {Field = "aggregationFields.creationPeriodYears005", Size = 10, Missing = "0", Order = order}, facetsFilters);
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.creationPeriodYears010")
                        {Field = "aggregationFields.creationPeriodYears005", Size = 10, Missing = "0", Order = order}, facetsFilters);
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.creationPeriodYears025")
                        {Field = "aggregationFields.creationPeriodYears025", Size = 10, Missing = "0", Order = order}, facetsFilters);
            aggregations &=
                CreateFacet(
                    new TermsAggregation("aggregationFields.creationPeriodYears100")
                        {Field = "aggregationFields.creationPeriodYears100", Size = int.MaxValue, Order = order, Missing = "0"}, facetsFilters);


            aggregations &= new FilterAggregation("bestellbare_einheiten")
            {
                Filter = new TermQuery {Field = "canBeOrdered", Value = "true"},
                Aggregations = new TermsAggregation("nach_level") {Field = "level.keyword"}
            };

            searchRequest.Aggregations = aggregations;
        }

        private static AggregationBase CreateFacet(AggregationBase primaryAggregation, FacetFilters[] facetsFilters)
        {
            var primaryAggregationName = ((IAggregation) primaryAggregation).Name;

            return new FilterAggregation("facet_" + primaryAggregationName)
            {
                Filter = CreateQuery(facetsFilters, Regex.Replace(primaryAggregationName, @"\d\d\d", "")),
                Aggregations = primaryAggregation
            };
        }

        private static QueryContainer CreateQuery(FacetFilters[] facetsFilters, string facetToExclude = null)
        {
            if (facetsFilters == null)
            {
                return new MatchAllQuery();
            }

            var list = new List<string>();

            foreach (var item in facetsFilters)
            {
                if (string.IsNullOrEmpty(item.Facet))
                {
                    throw new BadRequestException("Facet is not allowed to contain nothing.");
                }

                if (item.Facet != facetToExclude &&
                    item.Filters != null &&
                    item.Filters.Length != 0)
                {
                    var securedFilters = GetSecuredFacetFilters(item.Filters);

                    var filterForOneFacet = '(' + string.Join(" OR ", securedFilters) + ')';

                    list.Add(filterForOneFacet);
                }
            }

            if (list.Count == 0)
            {
                return new MatchAllQuery();
            }

            return new QueryStringQuery {Query = string.Join(" AND ", list)};
        }

        /// <summary>
        ///     Diese Methode hat die Aufgabe sicherzustellen, das kein Zugriff auf ein unerlaubtes Elastic Feld erfolgt.
        /// </summary>
        public static List<string> GetSecuredFacetFilters(string[] filterArray)
        {
            var secured = new List<string>();

            foreach (var filter in filterArray)
            {
                if (string.IsNullOrEmpty(filter))
                {
                    throw new BadRequestException("Filters array entry is not allowed to contain nothing.");
                }

                var splited = filter.Split(new[] {':'}, 2);

                if (splited.Length != 2)
                {
                    throw new BadRequestException("Every filters array entry must contain a colon.");
                }

                if (!IsFacetFilterLegal(splited))
                {
                    throw new BadRequestException("Filters contains an illegal field or syntax.");
                }

                secured.Add($"{splited[0]}:{splited[1].Escape()}");
            }

            return secured;
        }

        private static bool IsFacetFilterLegal(string[] splited)
        {
            if (allowedFacetFields.Contains(splited[0]))
            {
                return true;
            }

            if (splited[1].Length == 0)
            {
                return false;
            }

            var lastCharRemoved = splited[1].Remove(splited[1].Length - 1);

            return (splited[0] == "(_exists_" || splited[0] == "(!_exists_") &&
                   allowedFacetFields.Contains(lastCharRemoved) &&
                   splited[1].EndsWith(")");
        }

        private static void AddHighlighting(SearchRequest<ElasticArchiveRecord> searchRequest)
        {
            searchRequest.Highlight = new Highlight
            {
                PreTags = new[] {"<h1l1ght>"},
                PostTags = new[] {"</h1l1ght>"},
                Order = HighlighterOrder.Score,

                Fields = new Dictionary<Field, IHighlightField>
                {
                    {
                        "title",
                        new HighlightField
                        {
                            Field = "title",
                            NumberOfFragments = 0,
                            NoMatchSize = 0,
                            RequireFieldMatch = false
                        }
                    },
                    {
                        "all_Metadata_Text",
                        new HighlightField
                        {
                            Field = "all_Metadata_Text",
                            NumberOfFragments =
                                4, // Der Titel erscheint 3 mal falls er ein Treffer ist. 4 Einträge holen damit wir auch etwas anderes erhalten.
                            FragmentSize = 512,
                            RequireFieldMatch = true
                        }
                    },
                    {
                        "all_Primarydata",
                        new HighlightField
                        {
                            Field = "all_Primarydata",
                            NumberOfFragments = 1,
                            FragmentSize = 512,
                            RequireFieldMatch = true
                        }
                    }
                }
            };
        }

        private static void ExcludeUnwantedFields(SearchRequest<ElasticArchiveRecord> searchRequest)
        {
            // Exclude content from primarydata. 
            searchRequest.Source = new SourceFilter {Excludes = Infer.Fields("all", "primaryData.items.content")};
        }

        private void ProcessQueryResult<T>(ElasticQueryResult<T> result, FacetFilters[] facetsFilters, UserAccess access,
            IElasticsearchSerializer serializer) where T : TreeRecord
        {
            var response = result.Response;

            var hits = response?.Hits ?? new List<IHit<T>>();
            result.TotalNumberOfHits = response?.HitsMetadata != null ? (int) response.HitsMetadata.Total : -1;
            var entries = new List<Entity<T>>();
            foreach (var hit in hits)
            {
                var data = JsonConvert.DeserializeObject<T>(serializer.SerializeToString(hit.Source));

                var entry = new Entity<T>
                {
                    Data = data,
                    Highlight = GetHighlightingObj(hit, access),
                    Explanation = GetExplanationObj(hit, serializer)
                };

                if (access != null)
                {
                    entry.IsDownloadAllowed = access.HasAnyTokenFor(data.PrimaryDataDownloadAccessTokens);
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

        private static JObject GetExplanationObj<T>(IHit<T> hit, IElasticsearchSerializer serializer) where T : TreeRecord
        {
            if (hit?.Explanation?.Value == null)
            {
                return null;
            }

            var explanationsObj = new JObject
            {
                {"value", hit.Explanation.Value},
                {"explanation", GetExplanationWithObscuredValues(serializer.SerializeToString(hit.Explanation))}
            };

            return explanationsObj;
        }

        private static string GetExplanationWithObscuredValues(string serializedExplanation)
        {
            if (string.IsNullOrEmpty(serializedExplanation))
            {
                return serializedExplanation;
            }

            // Das Pattern findet alle KeyValuePairs, welche
            // 1. mit "metadataAccessTokens", "primaryDataDownloadAccessTokens" oder "primaryDataFulltextAccessTokens" beginnen
            // 2. dann kommt eine beliebige Anzahl an Leerzeichen
            // 3. dann kommt ein Doppelpunkt
            // 4. dann kommt einen beliebige Anzahl beliebiger Zeichen, bis entweder ein ',' oder ein '"' kommt
            // Achtung: der Regex ist Case-sensitive!
            const string pattern = "(metadataAccessTokens|primaryDataDownloadAccessTokens|primaryDataFulltextAccessTokens)[\\s]{0,}:.*?(?=(,|\"))";

            return new Regex(pattern).Replace(serializedExplanation, "***");
        }

        internal static JObject GetHighlightingObj<T>(IHit<T> hit, UserAccess access) where T : TreeRecord
        {
            if (hit.Highlights == null || !hit.Highlights.Any())
            {
                return null;
            }


            var titleHighlight = FindHighlights(hit, "title");
            var metaDataHighlight = FindHighlights(hit, "all_Metadata_Text")?.ToList();

            // Verhindern der Anzeige von geschützten Primärdaten im Snippet für unberechtigte User
            var primaryDataHighlight = access.HasAnyTokenFor(hit.Source.PrimaryDataFulltextAccessTokens)
                ? FindHighlights(hit, "all_Primarydata")
                : null;

            List<string> mostRelevantVektor;
            metaDataHighlight = TakeFirstNonTitleHighlight<T>(titleHighlight, metaDataHighlight);

            if (metaDataHighlight == null || !metaDataHighlight.Any())
            {
                mostRelevantVektor = primaryDataHighlight;
            }
            else
            {
                mostRelevantVektor = metaDataHighlight;
            }

            var highlightobj = new JObject
            {
                ["title"] = titleHighlight != null // titleHighlight kann null enthalten
                    ? new JArray(titleHighlight)
                    : new JArray(hit.Source.Title),
                ["mostRelevantVektor"] = mostRelevantVektor != null
                    ? new JArray(mostRelevantVektor)
                    : null
            };

            return highlightobj;
        }

        private static List<string> TakeFirstNonTitleHighlight<T>(List<string> titleHighlight, List<string> metaDataHighlight)
            where T : TreeRecord
        {
            if (metaDataHighlight == null || !metaDataHighlight.Any())
            {
                return new List<string>();
            }

            var enumerable = (IEnumerable<string>) metaDataHighlight;

            if (titleHighlight != null)
            {
                enumerable = metaDataHighlight.Where(e => !titleHighlight.First().Contains(e));
            }

            enumerable = enumerable.Take(1);
            return enumerable.ToList();
        }

        private static List<string> FindHighlights<T>(IHit<T> hit, string key) where T : TreeRecord
        {
            var foundHighlight = hit.Highlights.FirstOrDefault(kv => kv.Key == key);
            return foundHighlight.Value?.Highlights.ToList();
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
                else
                {
                    foreach (var item in itemCollection)
                    {
                        var key = string.IsNullOrWhiteSpace(item["keyAsString"]?.ToString())
                            ? item["key"]
                            : item["keyAsString"];

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
    }
}
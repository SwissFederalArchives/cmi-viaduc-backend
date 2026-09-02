using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.api.Templates;
using Elastic.Clients.Elasticsearch ;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SortOrder = Elastic.Clients.Elasticsearch.SortOrder;

namespace CMI.Web.Frontend.api.Elastic;

public interface ISearchRequestBuilder
{
    SearchRequest<ElasticArchiveRecord> Build(ElasticsearchClient client, ElasticQuery query, UserAccess access);
    BoolQuery GetQueryWithSecurity(ElasticsearchClient client, Query query, UserAccess access);
    void AddPaging(Paging paging, SearchRequest<ElasticArchiveRecord> searchRequest);
    void AddSort(string orderBy, string sortOrder, SearchRequest<ElasticArchiveRecord> searchRequest);
    void ExcludeUnwantedFields(SearchRequest<ElasticArchiveRecord> searchRequest, bool excludeInternalFields = true);
}

public class SearchRequestBuilder : ISearchRequestBuilder
{
    private static readonly string[] allowedFacetFields =
    {
        "level", "customFields.zugänglichkeitGemässBga", "aggregationFields.ordnungskomponenten", "aggregationFields.bestand",
        "aggregationFields.hasPrimaryData", "aggregationFields.creationPeriodYears001", "aggregationFields.creationPeriodYears005",
        "aggregationFields.creationPeriodYears010", "aggregationFields.creationPeriodYears025", "aggregationFields.creationPeriodYears100",
        "aggregationFields.protectionEndDateDossier.year"
    };

    private readonly IElasticSettings elasticSettings;
    private readonly List<TemplateField> internalFields;
    private readonly QueryTransformationService queryTransformationService;


    public SearchRequestBuilder(IElasticSettings elasticSettings, QueryTransformationService queryTransformationService,
        List<TemplateField> internalFields)
    {
        this.elasticSettings = elasticSettings;
        this.queryTransformationService = queryTransformationService;
        this.internalFields = internalFields;
    }

    public SearchRequest<ElasticArchiveRecord> Build(ElasticsearchClient client, ElasticQuery query, UserAccess access)
    {
        var request = new SearchRequest<ElasticArchiveRecord>(elasticSettings.DefaultIndex);

        var queryWithSecurity = GetQueryWithSecurity(client, query.Query, access);

        request.Query = queryWithSecurity;
        request.PostFilter = CreateQuery(query.SearchParameters.FacetsFilters);


        var parameters = query.SearchParameters;
        var options = parameters?.Options;

        AddPaging(parameters?.Paging, request);
        AddSort(parameters?.Paging?.OrderBy, parameters?.Paging?.SortOrder, request);

        // Internal fields are only visible for BAR role
        ExcludeUnwantedFields(request, !access.HasAnyTokenFor(new[] { AccessRolesEnum.BAR.ToString() }));

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

        request.TrackTotalHits = new TrackHits(true);

        return request;
    }

    public BoolQuery GetQueryWithSecurity(ElasticsearchClient client, Query queryContainer, UserAccess access)
    {
        if (access == null)
        {
            throw new ArgumentNullException(nameof(access));
        }

        if (access.CombinedTokens?.Length == 0)
        {
            throw new ArgumentException($"{nameof(access)} is not valid");
        }

        var queryWithMetadataAccessToken = GetQueryWithMetadataAccessToken(queryContainer, access);
      
        var queryWithProtectedFields = client.RequestResponseSerializer.Deserialize<Query>(
        queryTransformationService.TransformQuery(client.RequestResponseSerializer.SerializeToString(queryContainer)));
        if (queryWithProtectedFields != null)
        {
            var queryWithSecurityForAnonymization = GetQueryWithSecurityForAnonymization(queryWithProtectedFields, access);
            var boolQuery = new BoolQuery
            {
                Should = new List<Query>
                {
                    queryWithMetadataAccessToken,
                    queryWithSecurityForAnonymization
                }
            };
            return boolQuery;
        }

        return queryWithMetadataAccessToken;
    }

    public void AddPaging(Paging paging, SearchRequest<ElasticArchiveRecord> searchRequest)
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

    public void AddSort(string orderBy, string order, SearchRequest<ElasticArchiveRecord> searchRequest)
    {
        searchRequest.Sort = new List<SortOptions>();

        if (!string.IsNullOrEmpty(orderBy) && !string.IsNullOrEmpty(order))
        {
            SortOrder? sortOrder;
            switch (order.ToLowerInvariant())
            {
                case "ascending":
                    sortOrder = SortOrder.Asc;
                    break;

                case "descending":
                    sortOrder = SortOrder.Desc;
                    break;

                default:
                    throw new ArgumentException(
                        "The parameter sortOrder contains an invalid value. Valid values: 'ascending', 'descending', empty");
            }

            searchRequest.Sort ??= new List<SortOptions>();

            searchRequest.Sort.Add(new SortOptions
            {
                Field = new FieldSort(new Field(orderBy))
                {
                    Order = sortOrder
                }
            });

            // Falls teilweise nach mehreren Feldern sortiert werden soll, könnte eine Ergänzung wie folgt hinzugefügt werden:
            // if (orderBy == "treePath")
            // {
            //    searchRequest.Sort.Add(new FieldSort { Field = "treeSequence", Order = sortOrder });
            // }
        }

        searchRequest.Sort.Add(new SortOptions
        {
            Field = new FieldSort(new Field("_score"))
            {
                Order = SortOrder.Desc
            }
        });

       
        // Das folgende Sortierfeld ist ein "tie-breaker", damit die Reihenfolge immer klar definiert ist, auch wenn alle bisherigen Sort-Felder den gleichen Inhalt haben.
        // Die Reihenfolge muss definiert sein, damit das Paging funktioniert, denn wenn sich die Reihenfolge ändert zwischen zwei Seitenaufrufen,
        // wäre ein Paging sinnlos.
        searchRequest.Sort.Add(new SortOptions
        {
            Field = new FieldSort(new Field("referenceCode"))
            {
                Order = SortOrder.Asc
            }
        });
    }

    public void ExcludeUnwantedFields(SearchRequest<ElasticArchiveRecord> searchRequest, bool excludeInternalFields = true)
    {
        // Exclude content from primarydata. 
        var filter = new SourceFilter { Excludes = Infer.Fields("all", "primaryData.items.content") };
        if (excludeInternalFields)
        {
            internalFields.ForEach(f => filter.Excludes.And(Infer.Field(f.DbFieldName.ToLowerCamelCase())));
        }

        searchRequest.Source = new SourceConfig(filter);
    }

    private static void AddHighlighting(SearchRequest<ElasticArchiveRecord> searchRequest)
    {
        var fields = new Dictionary<Field, HighlightField>
        {
            {
                new Field("title"),
                new HighlightField
                {
                    NumberOfFragments = 0,
                    NoMatchSize = 0,
                    RequireFieldMatch = false
                }
            },
            {
                new Field("all_Metadata_Text"),
                new HighlightField
                {
                    NumberOfFragments =
                        4, // Der Titel erscheint 3 mal falls er ein Treffer ist. 4 Einträge holen damit wir auch etwas anderes erhalten.
                    FragmentSize = 512,
                    RequireFieldMatch = true
                }
            },
            {
                new Field("unanonymizedFields.title"),
                new HighlightField
                {
                    NumberOfFragments = 0,
                    NoMatchSize = 0,
                    RequireFieldMatch = false
                }
            },
            {
                new Field("protected_Metadata_Text"),
                new HighlightField
                {
                    NumberOfFragments =
                        4, // Der Titel erscheint 3 mal falls er ein Treffer ist. 4 Einträge holen damit wir auch etwas anderes erhalten.
                    FragmentSize = 512,
                    RequireFieldMatch = true
                }
            },
            {
                new Field("all_Primarydata"),
                new HighlightField
                {
                    NumberOfFragments = 1,
                    FragmentSize = 512,
                    RequireFieldMatch = true
                }
            }
        };
      
        searchRequest.Highlight = new Highlight(fields)
        {
            PreTags = new[] { "<h1l1ght>" },
            PostTags = new[] { "</h1l1ght>" },
            Order = HighlighterOrder.Score
        };
    }

    private static void AddAggregations(SearchRequest<ElasticArchiveRecord> searchRequest, FacetFilters[] facetsFilters)
    {
        var aggregations = new Dictionary<string, Aggregation>();

        aggregations.Add("facet_level", CreateFacet(new TermsAggregation { Field = "level.keyword", Size = int.MaxValue }, facetsFilters));

        aggregations.Add("facet_customFields.zugänglichkeitGemässBga", CreateFacet(new TermsAggregation { Field = "customFields.zugänglichkeitGemässBga", Size = 25 }, facetsFilters));

        aggregations.Add("facet_aggregationFields.ordnungskomponenten", CreateFacet(
             new TermsAggregation
             {
                 Field = "aggregationFields.ordnungskomponenten",
                 Size = facetsFilters != null && facetsFilters.Any(fac => fac.Facet.Equals("aggregationFields.ordnungskomponenten") && fac.ShowAll)
                     ? int.MaxValue
                     : 25
             }, facetsFilters));
        aggregations.Add("facet_aggregationFields.bestand", CreateFacet(new TermsAggregation
        {
            Field = "aggregationFields.bestand",
            Size = facetsFilters != null && facetsFilters.Any(fac => fac.Facet.Equals("aggregationFields.bestand") && fac.ShowAll)
                 ? int.MaxValue
                 : 25
        }, facetsFilters)); // Performance: Limit to 25
        aggregations.Add("facet_aggregationFields.hasPrimaryData",
             CreateFacet(new TermsAggregation() { Field = "aggregationFields.hasPrimaryData", Missing = "false" },
                 facetsFilters));

        // Zeitraum Filter
        // Für die feinen Filter reicht, wenn wir maximal 10 Stück zurückliefern. Da wir am Ende nur die Facette zurückliefern, die  weniger als 10 Buckets haben
        // var order = new List<TermsOrder> {new() {Key = "_term"}};
        var order = new List<KeyValuePair<Field, SortOrder>> { new(new Field("_key"), SortOrder.Asc) };
        aggregations.Add("facet_aggregationFields.creationPeriodYears001",
             CreateFacet(
                 new TermsAggregation
                 { Field = "aggregationFields.creationPeriodYears001", Size = 10, Missing = "0", Order = order }, facetsFilters));
        aggregations.Add("facet_aggregationFields.creationPeriodYears005",
             CreateFacet(
                 new TermsAggregation
                 { Field = "aggregationFields.creationPeriodYears005", Size = 10, Missing = "0", Order = order }, facetsFilters));
        aggregations.Add("facet_aggregationFields.creationPeriodYears010",
             CreateFacet(
                 new TermsAggregation
                 { Field = "aggregationFields.creationPeriodYears010", Size = 10, Missing = "0", Order = order }, facetsFilters));
        aggregations.Add("facet_aggregationFields.creationPeriodYears025",
             CreateFacet(
                 new TermsAggregation
                 { Field = "aggregationFields.creationPeriodYears025", Size = 10, Missing = "0", Order = order }, facetsFilters));
        aggregations.Add("facet_aggregationFields.creationPeriodYears100",
             CreateFacet(
                 new TermsAggregation
                 {
                     Field = "aggregationFields.creationPeriodYears100",
                     Size = int.MaxValue, Order = order, Missing = "0"
                 }, facetsFilters));

        aggregations.Add("facet_aggregationFields.protectionEndDateDossier.year",
             CreateFacet(
                 new TermsAggregation
                 {
                     Field = "aggregationFields.protectionEndDateDossier.year",
                     Size = facetsFilters != null && facetsFilters.Any(fac => fac.Facet.StartsWith("aggregationFields.protectionEndDateDossier") && fac.ShowAll)
                             ? int.MaxValue
                             : 5,
                     Order = order
                 }, facetsFilters));


        aggregations.Add( "bestellbare_einheiten", new Aggregation
            {
                Filter = new TermQuery(new Field("canBeOrdered"), FieldValue.Boolean(true)),
                Aggregations = new Dictionary<string, Aggregation>
                {
                    {
                        "bestellbare_einheiten",
                        new Aggregation
                        {
                            Terms = new TermsAggregation
                            {
                                Field = "canBeOrdered"
                        }
                    }
                }
                
            }
        });
        searchRequest.Aggregations = aggregations;
    }

    private static Aggregation CreateFacet(TermsAggregation primaryAggregation, FacetFilters[] facetsFilters)
    {
        var primaryAggregationName = primaryAggregation.Field.Name;
        return new Aggregation()
        {
            Filter = CreateQuery(facetsFilters, Regex.Replace(primaryAggregationName, @"\d\d\d", "")),
            Aggregations = new Dictionary<string, Aggregation>
            {
                {
                    primaryAggregationName,
                    new Aggregation
                    {
                        Terms = primaryAggregation
                    }
                }
            }
        };
    }


    private static Query CreateQuery(FacetFilters[] facetsFilters, string facetToExclude = null)
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


        return new QueryStringQuery(string.Join(" AND ", list));
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

            var splited = filter.Split(new[] { ':' }, 2);

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

    private static BoolQuery GetQueryWithMetadataAccessToken(Query querycontainer, UserAccess access)
    {
        var values = new List<FieldValue>();
        foreach (var token in access.CombinedTokens)
        {
            values.Add(token);
        }

        var termQuery = new TermsQuery(new Field("metadataAccessTokens"), new TermsQueryField(values));
        var queryWithSecurity = new BoolQuery
        {
            Must = new List<Query> { querycontainer },
            Filter = new List<Query> { termQuery }
        };

        return queryWithSecurity;
    }

    private static BoolQuery GetQueryWithSecurityForAnonymization(Query querycontainer, UserAccess access)
    {
        var values = new List<FieldValue>();
        foreach (var token in access.CombinedTokens)
        {
            values.Add(token);
        }

        var termQuery = new TermsQuery(new Field("fieldAccessTokens"), new TermsQueryField(values));

    
        var queryWithSecurity = new BoolQuery
        {
            Must = new List<Query> { querycontainer },
            Filter = new List<Query> { termQuery }
        };

        return queryWithSecurity;
    }
}
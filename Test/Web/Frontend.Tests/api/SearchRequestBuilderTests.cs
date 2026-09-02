using Autofac.Core;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.api.Templates;
using CMI.Web.Frontend.API.Tests.ElasticMock;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Security;
using Elastic.Transport;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.API.Tests.api;

public class SearchRequestBuilderTests
{
    private Mock<IElasticClientProvider> clientProvider;
    private ElasticServiceMock service;

    [Test]
    public void Internal_fields_are_excluded_if_user_has_OE1_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
          var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"}
        });

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
            It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery {Query = new MatchAllQuery()}, CreatingSimpleUser(AccessRoles.RoleOe1));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("customFields.bemerkungZurVe").ShouldBeTrue();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").ShouldBeTrue();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").ShouldBeTrue();
    }

    [Test]
    public void Internal_fields_are_excluded_if_user_has_OE3_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"},
            // Not really an internal field but to test if other fields than customFields are correctly excluded
            new() {DbFieldName = "WithinInfo"}
        });

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
            It.IsAny<ElasticQueryResult<DetailRecord>>()),  new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleOe3, null, null, false));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("customFields.bemerkungZurVe").ShouldBeTrue();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").ShouldBeTrue();
        filters.Excludes.Contains("withinInfo").ShouldBeTrue();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").ShouldBeTrue();
    }

    [Test]
    public void Internal_fields_are_not_excluded_if_user_has_BAR_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"},
            // Not really an internal field but to test if other fields than customFields are correctly excluded
            new() {DbFieldName = "WithinInfo"}
        });

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
            It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleBAR, null, null, false));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("CustomFields.BemerkungZurVe").ShouldBeFalse();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").ShouldBeFalse();
        filters.Excludes.Contains("withinInfo").ShouldBeFalse();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").ShouldBeTrue();
    }
     
    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_BAR_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
            It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleBAR, null, null, false));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull("");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("BAR"))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("AMA"))).ShouldBeFalse();


        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("EB_1"))).ShouldBeFalse("Because BAR users don't receive EB tokens for their userId");


        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("FG_1"))).ShouldBeFalse("Because BAR users don't receive FG tokens for their userId");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE1_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
            It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery { Query = new MatchAllQuery() }, new UserAccess("1", AccessRoles.RoleOe1, null, null, false));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();

        
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("Ö1"))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("BAR"))).ShouldBeFalse();

       test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("EB_1"))).ShouldBeFalse("Because Ö1 users don't receive EB tokens for their userId");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("FG_1"))).ShouldBeFalse("Because Ö1 users don't receive FG tokens for their userId");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE2_role_and_permissions()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();


        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
                It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery { Query = new MatchAllQuery() },
           CreatingSimpleUser(AccessRoles.RoleOe2));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();

        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("Ö2")))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("EB_123")))).ShouldBeFalse("Because Ö2 users don't receive EB tokens for their userId");



        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("EB_1"))).ShouldBeFalse("Because Ö2 users don't receive EB tokens for their userId");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Field.Name.Contains("FG_1"))).ShouldBeFalse("Because Ö2 users don't receive FG tokens for their userId");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE3_role_and_permissions()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
                It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery { Query = new MatchAllQuery() },
            CreatingSimpleUser(AccessRoles.RoleOe3));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();

        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("Ö3")))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("EB_123")))).ShouldBeTrue("Because Ö3 users receive EB tokens for their userId");
        
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("FG_123")))).ShouldBeTrue("Because Ö3 users receive FG tokens for their userId");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("BAR")))).ShouldBeFalse();
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE3_role_and_DDS_permissions()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
                It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery { Query = new MatchAllQuery() },
            CreatingSimpleUser(AccessRoles.RoleOe3, true));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();

        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("Ö3"))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("EB_123"))).ShouldBeFalse("Because DDS users don't receive EB tokens for their userId");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("FG_123"))).ShouldBeFalse("Because DDS users don't receive FG tokens for their userId");


        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("DDS")))).ShouldBeTrue("Because Ö3 users that pertain to researcher group only receive DDS tokens");
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.Any(v => v.Value.ToString().Contains("BAR")))).ShouldBeFalse();
    }

    [Test]
    public void Check_if_OE2_user_cannot_receive_DDS_permission()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(It.IsAny<IElasticSettings>(),
                It.IsAny<ElasticQueryResult<DetailRecord>>()), new ElasticQuery { Query = new MatchAllQuery() },
            CreatingSimpleUser(AccessRoles.RoleOe2));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();

        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("Ö2"))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("DDS"))).ShouldBeFalse("Because Ö2 users can't receive DDS tokens");
    }

    [Test]
    public void Check_if_BVW_user_cannot_receive_DDS_permission()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), new ElasticQuery { Query = new MatchAllQuery() },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        var test = result.Query;
        test.Bool.Should.First().ShouldNotBeNull("Because filter for access Tokens is needed");
        test.Bool.Should.First(f => f.Bool.Filter.First().Terms.Field.Name.Contains("metadataAccessTokens")).ShouldNotBeNull();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("BVW"))).ShouldBeTrue();
        test.Bool.Should.Any(f => f.Bool.Filter.Any(f => f.Terms.Terms.Value1.First().Value.ToString().Contains("DDS"))).ShouldBeFalse("Because BVW users can't receive DDS tokens");
    }

    [Test]
    public void Check_if_sorting_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), new ElasticQuery
        {
            Query = new MatchAllQuery(),
            SearchParameters = new SearchParameters { Paging = new Paging { OrderBy = "title", Skip = 10, Take = 10, SortOrder = "descending" } }
        },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Sort.Count.ShouldBe(3);
        result.Sort.ElementAt(0).Field.Field.Name.ShouldBe("title");
        result.Sort.ElementAt(1).Field.Order.ShouldBe(SortOrder.Desc);
        result.Sort.ElementAt(2).Field.Field.Name.ShouldBe("referenceCode");
    }

    [Test]
    public void Check_if_default_search_options_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()),
            new ElasticQuery
        {
            Query = new MatchAllQuery()
        },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Sort.Count.ShouldBe(2);
        result.Sort.ElementAt(0).Field.Order.ShouldBe(SortOrder.Desc);
        result.Sort.ElementAt(1).Field.Field.Name.ShouldBe("referenceCode");
    
        result.From.ShouldBeNull();
        result.Size.ShouldBeNull();

        result.Highlight.ShouldBeNull();
        result.Explain.ShouldBeNull();
        result.Aggregations.ShouldBeNull();
    }

    [Test]
    public void Check_if_paging_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), new ElasticQuery
        {
            Query = new MatchAllQuery(),
            SearchParameters = new SearchParameters { Paging = new Paging { Skip = 100, Take = 1000 } }
        },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.From.ShouldBe(100);
        result.Size.ShouldBe(1000);
    }

    [Test]
    public void Check_if_options_are_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), new ElasticQuery
        {
            Query = new MatchAllQuery(),
            SearchParameters = new SearchParameters { Options = new SearchOptions() { EnableExplanations = true, EnableAggregations = true, EnableHighlighting = true } }
        },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Highlight.ShouldNotBeNull();
        result.Highlight.Fields.Count.ShouldBe(5);
        result.Explain.ShouldBe(true);
        result.Aggregations.ShouldNotBeNull();
    }

    [Test]
    public void Check_if_added_Field_title_to_Searchquery_and_queryBuilder_duplicated_field_in_Unnonymized_query()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        var user = CreatingSimpleUser(AccessRoles.RoleOe3);
        var searchParameter = new SearchParameters
        {
            Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new ()
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "title", Value = "Haus"}
                        }
                    }

                }
            }
        };
        ElasticQueryBuilder.translatorsByKey = new Dictionary<string, IFieldTranslator>
        {
            { "title", new StandardTranslator() },
            { "referenceCode", new StandardTranslator() }

        };
        var query = new ElasticQuery
        {
            SearchParameters = searchParameter,
            Query = ElasticQueryBuilder.CreateQueryForSearchModel(searchParameter.Query, user)
        };

        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), query, user);

        // assert
        result.Query .Bool.ShouldNotBeNull();
        result.Query .Bool.Should.Count().ShouldBe(2);
        // test if the query was duplicated
        var stringQuery = GetStringQuery(result.Query .Bool.Should.First());
        stringQuery.Count.ShouldBe(1);
        stringQuery.First().Query.ShouldBe("Haus");
        stringQuery.First().DefaultField.Name.ShouldBe("title");
        stringQuery = GetStringQuery(result.Query .Bool.Should.Last());
        stringQuery.Count.ShouldBe(1);
        stringQuery.First().Query.ShouldBe("Haus");
        stringQuery.First().DefaultField.Name.ShouldBe("unanonymizedFields.title");
    }

    [Test]
    public void Check_if_added_unknown_Field_husler_in_searchQuery_throw_Exception()
    {
        // arrange

        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        var user = CreatingSimpleUser(AccessRoles.RoleOe2);
        var searchParameter = new SearchParameters
        {
            Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new ()
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "husler", Value = "Ball"}
                        }
                    }

                }
            }
        };
        ElasticQueryBuilder.translatorsByKey = new Dictionary<string, IFieldTranslator> { { "husler", new StandardTranslator() } };
        var query = new ElasticQuery
        {
            SearchParameters = searchParameter,
            Query = ElasticQueryBuilder.CreateQueryForSearchModel(searchParameter.Query, user)
        };

        Assert.Throws<ArgumentException>(() => builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), query, user));
    }

    [Test]
    public void Check_if_added_Field_referenceCode_to__searchQuery_field_was_not_duplicated_in_Unnonymized_Fieldquery()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        var user = CreatingSimpleUser(AccessRoles.RoleOe2);

        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var searchParameter = new SearchParameters
        {
            Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new ()
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "referenceCode", Value = "Ball"}
                        }
                    }

                }
            }
        };
        ElasticQueryBuilder.translatorsByKey = new Dictionary<string, IFieldTranslator>
        {
            { "husler", new StandardTranslator() },
            { "referenceCode", new StandardTranslator() }

        };
        var query = new ElasticQuery
        {
            SearchParameters = searchParameter,
            Query = ElasticQueryBuilder.CreateQueryForSearchModel(searchParameter.Query, user)
        };
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), query, user);
        // assert
        result.Query.Bool.ShouldNotBeNull();
        result.Query.Bool.Should.Count().ShouldBe(2);
        var stringQuery = GetStringQuery(result.Query.Bool.Should.First());
        stringQuery.Count.ShouldBe(1);
        stringQuery.First().Query.ShouldBe("Ball");
        stringQuery.First().DefaultField.Name.ShouldBe("referenceCode");
    }

    [Test]
    public void Check_if_added_two_Fields_referenceCode_and_title_to_searchQuery_only_title_was_duplicated_with_Unnonymized_Fieldquery()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        var user = CreatingSimpleUser(AccessRoles.RoleOe2);
        var searchParameter = new SearchParameters
        {
            Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new ()
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "referenceCode", Value = "Ball"},
                            new SearchField {Key = "title", Value = "Katze"}
                        }
                    }

                }
            }
        };
        ElasticQueryBuilder.translatorsByKey = new Dictionary<string, IFieldTranslator>
        {
            { "title", new StandardTranslator() },
            { "referenceCode", new StandardTranslator() }

        };
        var query = new ElasticQuery
        {
            SearchParameters = searchParameter,
            Query = ElasticQueryBuilder.CreateQueryForSearchModel(searchParameter.Query, user)
        };
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), query, user);
        // assert
        result.Query .Bool.ShouldNotBeNull();
        result.Query .Bool.Should.Count().ShouldBe(2);
        var stringQuery = GetStringQuery(result.Query .Bool.Should.First());
        stringQuery.Count.ShouldBe(2);
        stringQuery.First().Query.ShouldBe("Ball");
        stringQuery.First().DefaultField.Name.ShouldBe("referenceCode");
        stringQuery.Last().Query.ShouldBe("Katze");
        stringQuery.Last().DefaultField.Name.ShouldBe("title");
        stringQuery = GetStringQuery(result.Query .Bool.Should.Last());
        stringQuery.First().Query.ShouldBe("Ball");
        stringQuery.First().DefaultField.Name.ShouldBe("referenceCode");
        stringQuery.Last().Query.ShouldBe("Katze");
        stringQuery.Last().DefaultField.Name.ShouldBe("unanonymizedFields.title");
    }

    [Test]
    public void Check_if_added_two_Fields_allMetaData_and_title_to_searchQuery_both_fields_duplicated_in_Unnonymized_Fieldquery()
    {
        // arrange
        InitializeElasticClient(CreateMockResponse(new List<ElasticArchiveRecord>()));
        var builder = CreatingSimpleSearchRequestBuilder();
        var user = CreatingSimpleUser(AccessRoles.RoleOe2);
        var searchParameter = new SearchParameters
        {
            Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new ()
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "allMetaData", Value = "Die Katze ist im Haus"},
                            new SearchField {Key = "title", Value = "Katze"}
                        }
                    }

                }
            }
        };
        ElasticQueryBuilder.translatorsByKey = new Dictionary<string, IFieldTranslator>
        {
            { "title", new StandardTranslator() },
            { "allMetaData", new AllMetaDataTranslator() }

        };
        var query = new ElasticQuery
        {
            SearchParameters = searchParameter,
            Query = ElasticQueryBuilder.CreateQueryForSearchModel(searchParameter.Query, user)
        };
        // act
        var result = builder.Build(clientProvider.Object.GetElasticClient(new ElasticSettings(), new ElasticQueryResult<TreeRecord>()), query, user);

        // assert
        result.Query .Bool.ShouldNotBeNull();
        result.Query .Bool.Should.Count().ShouldBe(2);
        var stringQuery = GetStringQuery(result.Query.Bool.Should.First());
        stringQuery.Count.ShouldBe(2);
        stringQuery.First().Query.ShouldBe(@"all_Metadata_\*:(Die Katze ist im Haus)");
        stringQuery.First().DefaultField.ShouldBeNull("all_Metadata has no Field");
        stringQuery.Last().Query.ShouldBe("Katze");
        stringQuery.Last().DefaultField.Name.ShouldBe("title");
        //  Duplicated query
        stringQuery = GetStringQuery(result.Query .Bool.Should.Last());

        stringQuery.Count.ShouldBe(2);
        stringQuery.First().Query.ShouldBe(@"protected_Metadata_Text\*:(Die Katze ist im Haus)");
        stringQuery.First().DefaultField.ShouldBeNull("protected Metadata_Text has no Field");
        stringQuery.Last().Query.ShouldBe("Katze");
        stringQuery.Last().DefaultField.Name.ShouldBe("unanonymizedFields.title");
    }

    private static List<QueryStringQuery> GetStringQuery(Query firstQueryContainer)
    {
        List<QueryStringQuery> result = new List<QueryStringQuery>();
        var betweenContainer = firstQueryContainer;
        var betweenContainer2 = betweenContainer.Bool.Must.First();
        foreach (var queryContainer in betweenContainer2.Bool.Must)
        {
            foreach (var queryContainer2 in queryContainer.Bool.Must)
            {
                result.Add(queryContainer2.QueryString);
            }
        }

        return result;
    }

    private SearchRequestBuilder CreatingSimpleSearchRequestBuilder()
    {
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(ReadSearchSettings()), new List<TemplateField>());
        return builder;
    }

    private UserAccess CreatingSimpleUser(string role, bool researcherGroue = false)
    {
        return new UserAccess("123", role, null, null, researcherGroue);
    }

    private static SearchSetting ReadSearchSettings()
    {
        var newSettings = new JObject();
        var path = AppDomain.CurrentDomain.BaseDirectory + "Resources//settings.json";
        if (File.Exists(path))
        {
            var clientSettings = JsonHelper.GetJsonFromFile(path);
            if (clientSettings != null)
            {
                SettingsHelper.UpdateSettingsWith(newSettings, clientSettings, true);
            }
        }

        return SettingsHelper.GetSettingsFor<SearchSetting>(newSettings, "search");
    }


    private void InitializeElasticClient(SearchResponse<ElasticArchiveRecord> response)
    {
        var clientSearchForId = new Mock<ElasticsearchClient>();

        clientProvider = new Mock<IElasticClientProvider>();
        clientProvider.Setup(m =>
            m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>())).Returns(clientSearchForId.Object);
        clientProvider.Setup(m =>
            m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>())).Returns(clientSearchForId.Object);
        clientProvider.Setup(m =>
            m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>())).Returns(clientSearchForId.Object);

        var translatorMock = new Mock<ITranslator>();

        translatorMock.Setup(f => f.GetTranslation("de", "search.termToShort", It.IsAny<string>()))
            .Returns("search.termToShort");
        translatorMock.Setup(f => f.GetTranslation("de", "search.termToShortForAll", It.IsAny<string>()))
            .Returns("search.termToShortForAll");
        var srb = new Mock<ISearchRequestBuilder>();

        var request = new Mock<SearchRequest<ElasticArchiveRecord>>();
        srb.Setup(s => s.Build(It.IsAny<ElasticsearchClient>(), It.IsAny<ElasticQuery>(), It.IsAny<UserAccess>())).Returns(request.Object);


        clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveRecord>
            (It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

        translatorMock.Setup(f =>
                f.GetTranslation("de", "search.termToShort", It.IsAny<string>()))
            .Returns("search.termToShort");

        translatorMock.Setup(f =>
                f.GetTranslation("de", "search.termToShortForAll", It.IsAny<string>()))
            .Returns("search.termToShortForAll");

        service = new ElasticServiceMock(
            clientProvider.Object,
            srb.Object,
            new ElasticSettings(),
            new List<TemplateField>(), null);
    }

    private static SearchResponse<ElasticArchiveRecord> CreateMockResponse(List<ElasticArchiveRecord> list)
    {
        var hitList = new List<Hit<ElasticArchiveRecord>>();
        foreach (var item in list)
        {
            var hit = new Hit<ElasticArchiveRecord>(item.ArchiveRecordId, "test-index")
            {
                Source = item
            };
            hitList.Add(hit);
        }
        var temp = new SearchResponse<ElasticArchiveRecord>
        {
            HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
        };

        var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

        return searchResponse;
    }


}
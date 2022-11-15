using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.api.Templates;
using FluentAssertions;
using Moq;
using Nest;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api;

public class SearchRequestBuilderTests
{
    [Test]
    public void Internal_fields_are_excluded_if_user_has_OE1_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"}
        });

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()}, CreatingSimpleUser(AccessRoles.RoleOe1));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("customFields.bemerkungZurVe").Should().BeTrue();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").Should().BeTrue();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").Should().BeTrue();
    }

    [Test]
    public void Internal_fields_are_excluded_if_user_has_OE3_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"},
            // Not really an internal field but to test if other fields than customFields are correctly excluded
            new() {DbFieldName = "WithinInfo"}
        });

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleOe3, null, null, false));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("customFields.bemerkungZurVe").Should().BeTrue();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").Should().BeTrue();
        filters.Excludes.Contains("withinInfo").Should().BeTrue();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").Should().BeTrue();
    }

    [Test]
    public void Internal_fields_are_not_excluded_if_user_has_BAR_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>
        {
            new() {DbFieldName = "CustomFields.BemerkungZurVe"},
            new() {DbFieldName = "CustomFields.EntstehungszeitraumAnmerkung"},
            // Not really an internal field but to test if other fields than customFields are correctly excluded
            new() {DbFieldName = "WithinInfo"}
        });

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleBAR, null, null, false));

        // assert
        var filters = result.Source.Match(a => null, b => b);
        filters.Excludes.Contains("CustomFields.BemerkungZurVe").Should().BeFalse();
        filters.Excludes.Contains("customFields.entstehungszeitraumAnmerkung").Should().BeFalse();
        filters.Excludes.Contains("withinInfo").Should().BeFalse();
        // Always exclude this field
        filters.Excludes.Contains("primaryData.items.content").Should().BeTrue();
    }
     
    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_BAR_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleBAR, null, null, false));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BAR").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("EB_1").Should()
            .BeFalse("Because BAR users don't receive EB tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("FG_1").Should()
            .BeFalse("Because BAR users don't receive FG tokens for their userId");
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE1_role()
    {
        // arrange
        var elasticSettings = new Mock<IElasticSettings>();
        var builder = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()}, new UserAccess("1", AccessRoles.RoleOe1, null, null, false));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("Ö1").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BAR").Should().BeFalse();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("EB_1").Should()
            .BeFalse("Because Ö1 users don't receive EB tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("FG_1").Should()
            .BeFalse("Because Ö1 users don't receive FG tokens for their userId");

        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE2_role_and_permissions()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();

        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()},
           CreatingSimpleUser(AccessRoles.RoleOe2));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("Ö2").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("EB_123").Should()
            .BeFalse("Because Ö2 users don't receive EB tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("FG_123").Should()
            .BeTrue("Because Ö2 users receive tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BAR").Should().BeFalse();
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE3_role_and_permissions()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()},
            CreatingSimpleUser(AccessRoles.RoleOe3));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("Ö3").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("EB_123").Should()
            .BeTrue("Because Ö3 users receive EB tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("FG_123").Should()
            .BeTrue("Because Ö3 users receive FG tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BAR").Should().BeFalse();
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_search_filter_for_metadataAccessTokens_is_correctly_set_for_OE3_role_and_DDS_permissions()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()},
            CreatingSimpleUser(AccessRoles.RoleOe3, true));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("Ö3").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("EB_123").Should()
            .BeFalse("Because DDS users don't receive EB tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("FG_123").Should()
            .BeFalse("Because DDS users don't receive FG tokens for their userId");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("DDS").Should()
            .BeTrue("Because Ö3 users that pertain to researcher group only receive DDS tokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BAR").Should().BeFalse();
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_OE2_user_cannot_receive_DDS_permission()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()},
            CreatingSimpleUser(AccessRoles.RoleOe2));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("Ö2").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("DDS").Should().BeFalse("Because Ö2 users can't receive DDS tokens");
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_BVW_user_cannot_receive_DDS_permission()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery {Query = new MatchAllQuery()},
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        var test = result.Query as IQueryContainer;
        test.Bool.Filter.Should().NotBeEmpty("Because filter for access Tokens is needed");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Field.Name.Should().Match("metadataAccessTokens");
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("BVW").Should().BeTrue();
        (test.Bool.Filter.First() as IQueryContainer).Terms.Terms.Contains("DDS").Should().BeFalse("Because BVW users can't receive DDS tokens");
        (test.Bool.Must.First() as IQueryContainer).MatchAll.Should().NotBeNull("Because this contains the original query");
    }

    [Test]
    public void Check_if_sorting_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery
            {
                Query = new MatchAllQuery(),
                SearchParameters = new SearchParameters {Paging = new Paging {OrderBy = "title", Skip = 10, Take = 10, SortOrder = "descending"}}
            },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Sort[0].Order.Should().Be(SortOrder.Descending);
        result.Sort[0].SortKey.Name.Should().Be("title");
        result.Sort[1].Order.Should().Be(SortOrder.Descending);
        result.Sort[1].SortKey.Name.Should().Be("_score", "Score is always added");
        result.Sort[2].Order.Should().Be(SortOrder.Ascending);
        result.Sort[2].SortKey.Name.Should().Be("referenceCode", "referenceCode is always added as tie breaker");
    }

    [Test]
    public void Check_if_default_search_options_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery
            {
                Query = new MatchAllQuery()
            },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Sort[0].Order.Should().Be(SortOrder.Descending);
        result.Sort[0].SortKey.Name.Should().Be("_score", "Score is always added");
        result.Sort[1].Order.Should().Be(SortOrder.Ascending);
        result.Sort[1].SortKey.Name.Should().Be("referenceCode", "referenceCode is always added as tie breaker");

        result.From.Should().BeNull();
        result.Size.Should().BeNull();

        result.Highlight.Should().BeNull();
        result.Explain.Should().BeNull();
        result.Aggregations.Should().BeNull();
    }

    [Test]
    public void Check_if_paging_is_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery
            {
                Query = new MatchAllQuery(),
                SearchParameters = new SearchParameters {Paging = new Paging {Skip = 100, Take = 1000}}
            },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.From.Should().Be(100);
        result.Size.Should().Be(1000);
    }

    [Test]
    public void Check_if_options_are_correctly_added_to_query()
    {
        // arrange
        var builder = CreatingSimpleSearchRequestBuilder();
        // act
        var result = builder.Build(new ElasticQuery
            {
                Query = new MatchAllQuery(),
                SearchParameters = new SearchParameters { Options = new SearchOptions(){EnableExplanations = true, EnableAggregations = true, EnableHighlighting = true}}
            },
            CreatingSimpleUser(AccessRoles.RoleBVW));

        // assert
        result.Highlight.Should().NotBeNull();
        result.Highlight.Fields.Count.Should().Be(5);
        result.Explain.Should().BeTrue();
        result.Aggregations.Should().NotBeNull();
    }


    [Test]
    public void Check_if_added_Field_title_to_Searchquery_and_queryBuilder_duplicated_field_in_Unnonymized_query()
    {
        // arrange
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
        var result = builder.Build(query, user);

        // assert
        (result.Query as IQueryContainer).Bool.Should().NotBeNull();
        (result.Query as IQueryContainer).Bool.Should.Count().Should().Be(2);
        // test if the query was duplicated
        var stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.First());
        stringQuery.Count.Should().Be(1);
        stringQuery.First().Query.Should().Be("Haus");
        stringQuery.First().DefaultField.Name.Should().Be("title");
        stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.Last());
        stringQuery.Count.Should().Be(1);
        stringQuery.First().Query.Should().Be("Haus");
        stringQuery.First().DefaultField.Name.Should().Be("unanonymizedFields.title");
    }

    [Test]
    public void Check_if_added_unknown_Field_husler_in_searchQuery_throw_Exception()
    {
        // arrange
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
        
        Assert.Throws<ArgumentException>(() => builder.Build(query, user));
    }

    [Test]
    public void Check_if_added_Field_referenceCode_to__searchQuery_field_was_not_duplicated_in_Unnonymized_Fieldquery()
    {
        // arrange
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
        var result = builder.Build(query, user);
        // assert
        (result.Query as IQueryContainer).Bool.Must.Should().NotBeNull();
        (result.Query as IQueryContainer).Bool.Must.Count().Should().Be(1, "no duplicated");
        var stringQuery = GetStringQuery(result.Query);
        stringQuery.Count.Should().Be(1);
        stringQuery.First().Query.Should().Be("Ball");
        stringQuery.First().DefaultField.Name.Should().Be("referenceCode");
    }
    
    [Test]
    public void Check_if_added_two_Fields_referenceCode_and_title_to_searchQuery_only_title_was_duplicated_in_Unnonymized_Fieldquery()
    {
        // arrange
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
        var result = builder.Build(query, user);
        // assert
        (result.Query as IQueryContainer).Bool.Should().NotBeNull();
        (result.Query as IQueryContainer).Bool.Should.Count().Should().Be(2);
        var stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.First());
        stringQuery.Count.Should().Be(2);
        stringQuery.First().Query.Should().Be("Ball");
        stringQuery.First().DefaultField.Name.Should().Be("referenceCode");
        stringQuery.Last().Query.Should().Be("Katze");
        stringQuery.Last().DefaultField.Name.Should().Be("title");
        stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.Last());
        stringQuery.First().Query.Should().Be("Katze");
        stringQuery.First().DefaultField.Name.Should().Be("unanonymizedFields.title");
    }


    [Test]
    public void Check_if_added_two_Fields_allMetaData_and_title_to_searchQuery_both_fields_duplicated_in_Unnonymized_Fieldquery()
    {
        // arrange
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
        var result = builder.Build(query, user);
        // assert
        (result.Query as IQueryContainer).Bool.Should().NotBeNull();
        (result.Query as IQueryContainer).Bool.Should.Count().Should().Be(2);
        var stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.First());
        stringQuery.Count.Should().Be(2);
        stringQuery.First().Query.Should().Be(@"all_Metadata_\*:(Die Katze ist im Haus)");
        stringQuery.First().DefaultField.Should().BeNull("all_Metadata has no Field");
        stringQuery.Last().Query.Should().Be("Katze");
        stringQuery.Last().DefaultField.Name.Should().Be("title");
        //  Duplicated query
        stringQuery = GetStringQuery((result.Query as IQueryContainer).Bool.Should.Last());

        stringQuery.Count.Should().Be(2);
        stringQuery.First().Query.Should().Be(@"protected_Metadata_Text\*:(Die Katze ist im Haus)");
        stringQuery.First().DefaultField.Should().BeNull("protected Metadata_Text has no Field");
        stringQuery.Last().Query.Should().Be("Katze");
        stringQuery.Last().DefaultField.Name.Should().Be("unanonymizedFields.title");
    }

    private static List<IQueryStringQuery> GetStringQuery(IQueryContainer firstQueryContainer)
    {
        List<IQueryStringQuery> result = new List<IQueryStringQuery>();
        var betweenContainer = firstQueryContainer.Bool.Must.First();
        var betweenContainer2 = (betweenContainer as IQueryContainer).Bool.Must.First();
        foreach (var queryContainer in (betweenContainer2 as IQueryContainer).Bool.Must)
        {
            result.Add((queryContainer as IQueryContainer).QueryString);
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

}
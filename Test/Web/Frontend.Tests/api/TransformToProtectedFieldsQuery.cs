using System;
using System.Collections.Generic;
using System.IO;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Search;
using FluentAssertions;
using Nest;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class TransformToProtectedFieldsQueryTests
    {
        [Test]
        public void SearchRequest_for_customFields_zusatzkomponenteZac1_is_duplicated_for_unanonymized_field()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var searchField = new SearchField
            {
                Key = "customFields.zusatzkomponenteZac1",
                Value = "Zusatz"
            };

            var result = transformQueryToProtectedFields.TransformQuery(new QueryStringQuery
            {
                Query = searchField.Value.Escape(searchField.Key),
                DefaultField = searchField.Key,
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            });
            var serializer = new QueryContainerJsonConverter();
            var resultQueryText = serializer.Serialize(result);

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.zusatzkomponenteZac1\"");
            resultQueryText.Should().Contain("\"query\":\"Zusatz\"");
        }

        [Test]
        public void SearchRequest_for_title_is_duplicated_for_unanonymized_field()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var searchField = new SearchField
            {
                Key = "title",
                Value = "Oberzolldirektion Zentrale Ablage"
            };

            var result = transformQueryToProtectedFields.TransformQuery(new QueryStringQuery
            {
                Query = searchField.Value.Escape(searchField.Key),
                DefaultField = searchField.Key,
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            });
            var serializer = new QueryContainerJsonConverter();
            var resultQueryText = serializer.Serialize(result);

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.title\"");
            resultQueryText.Should().Contain("\"query\":\"Oberzolldirektion Zentrale Ablage\"");
        }

        [Test]
        public void SearchRequest_for_undefined_Field_throw_Exception()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var searchField = new SearchField
            {
                Key = "Feld",
                Value = "Dezentrale Ablage"
            };
            Assert.Throws<ArgumentException>(() => transformQueryToProtectedFields.TransformQuery(new QueryStringQuery
            {
                Query = searchField.Value.Escape(searchField.Key),
                DefaultField = searchField.Key,
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            }));
        }

        [Test]
        public void SearchRequest_for_multi_Fields_is_duplicated_for_unanonymized_fields()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var boolQuery = new BoolQuery();
            var queries = new List<QueryContainer>
            {
                new QueryStringQuery
                {
                    Query = "Oberzolldirektion Zentrale Ablage".Escape("title"),
                    DefaultField = "title",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = "Klaus Dieter".Escape("customFields.zusatzkomponenteZac1"),
                    DefaultField = "customFields.zusatzkomponenteZac1",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };

            boolQuery.Must = queries;
            var result = transformQueryToProtectedFields.TransformQuery(boolQuery);
            var serializer = new QueryContainerJsonConverter();
            var resultQueryText = serializer.Serialize(result);

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.title\"");
            resultQueryText.Should().Contain("\"query\":\"Oberzolldirektion Zentrale Ablage\"");

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.zusatzkomponenteZac1\"");
            resultQueryText.Should().Contain("\"query\":\"Klaus Dieter\"");
        }

        [Test]
        public void SearchRequest_for_multi_Fields_and_not_all_must_be_duplicated_for_unanonymized_fields()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var boolQuery = new BoolQuery();
            var queries = new List<QueryContainer>
            {
                new QueryStringQuery
                {
                    Query = "Geheime Sache".Escape("title"),
                    DefaultField = "title",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = "XY0815".Escape("referenceCode"),
                    DefaultField = "referenceCode",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = "Ich bin der Test Text".Escape("customFields.zusatzkomponenteZac1"),
                    DefaultField = "customFields.zusatzkomponenteZac1",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };

            boolQuery.Must = queries;
            var result = transformQueryToProtectedFields.TransformQuery(boolQuery);
            var serializer = new QueryContainerJsonConverter();
            var resultQueryText = serializer.Serialize(result);

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.title\"");
            resultQueryText.Should().Contain("\"query\":\"Geheime Sache\"");

            resultQueryText.Should().Contain("default_field\":\"unanonymizedFields.zusatzkomponenteZac1\"");
            resultQueryText.Should().Contain("\"query\":\"Ich bin der Test Text\"");

            resultQueryText.Should().NotContain("default_field\":\"unanonymizedFields.referenceCode\"");
            resultQueryText.Should().NotContain("default_field\":\"referenceCode\"");
            resultQueryText.Should().NotContain("\"query\":\"XY0815\"");
        }

        [Test]
        public void SearchRequest_for_not_protected_Fields()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var boolQuery = new BoolQuery();
            var queries = new List<QueryContainer>
            {
                new QueryStringQuery
                {
                    Query = "Geheime Sache".Escape("formerReferenceCode"),
                    DefaultField = "formerReferenceCode",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = "XY0815".Escape("referenceCode"),
                    DefaultField = "referenceCode",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = "Ich bin der Test Text".Escape("customFields.aktenzeichen"),
                    DefaultField = "customFields.aktenzeichen",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };

            boolQuery.Must = queries;
            var result = transformQueryToProtectedFields.TransformQuery(boolQuery);
            result.Should().BeNull();
        }

        [Test]
        public void SearchRequest_for_all_Fields()
        {
            var transformQueryToProtectedFields = new QueryTransformationService(ReadSearchSettings());
            var boolQuery = new BoolQuery();
            var queries = new List<QueryContainer>
            {
                new QueryStringQuery
                {
                    Query = @"all_\*:Ball",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                },
                new QueryStringQuery
                {
                    Query = @"all_Metadata_\*:Hund",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };

            boolQuery.Must = queries;
            var result = transformQueryToProtectedFields.TransformQuery(boolQuery);
            var serializer = new QueryContainerJsonConverter();
            var resultQueryText = serializer.Serialize(result);
            resultQueryText.Should().Contain("protected_Metadata_Text\\\\*:Hund");
            resultQueryText.Should().Contain("protected_Metadata_Text\\\\*:Ball");
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
}

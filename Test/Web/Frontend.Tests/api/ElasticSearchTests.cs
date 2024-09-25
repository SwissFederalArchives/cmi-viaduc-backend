using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Templates;
using Elasticsearch.Net;
using FluentAssertions;
using Moq;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    public class ElasticSearchTests
    {
        #region Fields
        private ElasticService service;
        private InMemoryConnection connection;
        private InMemoryConnection connectionForId;
        private Mock<IElasticClientProvider> clientProvider;
        #endregion

        #region Tests

        [Test]
        public void If_result_does_not_contain_anonymized_records_no_search_for_record_details_is_issued()
        {
            // arrange
            var data = new List<TreeRecord>
            {
                new ElasticArchiveRecord {ArchiveRecordId = "12345", Title = "Hund", All = "Ball"},
                new ElasticArchiveRecord {ArchiveRecordId = "12346", Title = "Ball", All = "Hund"}
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData(new List<TreeRecord> { data.Last() }));
            InitializeElasticClient();

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = this.service.RunQuery<TreeRecord>(CreateElasticQuery(), userAccess);
            
            //assert
            result.Response.Hits.Count.Should().Be(2);
            result.Response.Documents.Should().BeEquivalentTo(data);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>()), Times.Once);
            // Search for details is not executed
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Never);
        }

        [Test]
        public void Test_if_user_with_OE3_role_fetches_the_details_only_for_the_record_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");
          
            var data = new List<TreeRecord>
            {
                // OE3 user has no rights for this record
                new ElasticArchiveDbRecord {ArchiveRecordId = "12345", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleBAR }},
                // This record is not anonymized
                new ElasticArchiveRecord {ArchiveRecordId = "12353", Title = "Ball", All = "Hund", CustomFields = customFields},
                // OE3 user has rights for this record
                new ElasticArchiveDbRecord {ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleOe3, AccessRoles.RoleBAR, },
                UnanonymizedFields = new()
                {
                    Title = "Unanonymized Title"
                }
            }
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(
                GetInMemoryData(new List<TreeRecord> {data.Last()}));

            InitializeElasticClient();
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = service.RunQuery<TreeRecord>(CreateElasticQuery(), userAccess);

            // assert
            result.Response.Hits.Count.Should().Be(3);
            result.Response.Documents.Should().BeEquivalentTo(data);
            result.Data.Items.Last().Data.Title.Should().Be("Unanonymized Title");
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>()), Times.Once);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Once);
        }

        [Test]
        public void Test_if_user_with_BAR_role_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var data = new List<TreeRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "12345", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Hund"
                    }
                },
                new ElasticArchiveRecord {ArchiveRecordId = "12353", Title = "Ball", All = "Hund", CustomFields = customFields},
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR, AccessRoles.RoleOe3},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Title"
                    }
                }
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData(new List<TreeRecord> { data.Last() }));
            InitializeElasticClient();

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            
            // act
            var result = this.service.RunQuery<TreeRecord>(CreateElasticQuery(), userAccess);

            // assert
            result.Response.Hits.Count.Should().Be(3);
            result.Response.Documents.Should().BeEquivalentTo(data);

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.First().Data.Title.Should().Be("Unanonymized Title");
            result.Data.Items.Last().Data.Title.Should().Be("Unanonymized Title");
            result.Data.Items.Where(h => h.Data.IsAnonymized).All(h => h.Data.Title.Equals("Unanonymized Title")).Should().BeTrue();

            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>()), Times.Once);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Exactly(2));
        }

        [Test]
        public void Test_if_user_with_OE3_role_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var data = new List<TreeRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "12345", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleBAR },
                    UnanonymizedFields = new()
                    {
                        Title = "Geheimer Titel"
                    }
                },
                new ElasticArchiveDbRecord {ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleBAR, AccessRoles.RoleOe3 },
                UnanonymizedFields = new()
                {
                    Title = "Geheimer Titel"
                }
            }
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData(new List<TreeRecord> { data.Last() }));
            InitializeElasticClient();

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = service.RunQuery<TreeRecord>(CreateElasticQuery(), userAccess);

            // assert
            result.Response.Hits.Count.Should().Be(2);
            result.Response.Documents.Should().BeEquivalentTo(data);
            result.Data.Items.All(h => h.Data.Title.Equals("Geheimer Titel")).Should().BeFalse();

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.Last().Data.Title.Should().Be("Geheimer Titel");
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>()), Times.Once);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Once);
        }

        [Test]
        public void Test_if_user_with_Oe3_role_with_EB_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var data = new List<TreeRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "12345", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {"EB_S31830999", AccessRoles.RoleBAR},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Hund"
                    }
                },
                new ElasticArchiveRecord {ArchiveRecordId = "12353", Title = "Ball", All = "Hund", CustomFields = customFields},
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {"EG_S31830999", AccessRoles.RoleBAR, AccessRoles.RoleOe3},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Title"
                    }
                }
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData(new List<TreeRecord> { data.Last() }));
            InitializeElasticClient();
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = this.service.RunQuery<TreeRecord>(CreateElasticQuery(), userAccess);

            // assert
            result.Response.Hits.Count.Should().Be(3);
            result.Response.Documents.Should().BeEquivalentTo(data);

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.First().Data.Title.Should().Be("Unanonymized Title"); 
            result.Data.Items.Last().Data.Title.Should().Be("Unanonymized Title");

            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>()), Times.Once);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Exactly(2));
        }

        [Test]
        public void Test_if_query_for_id_is_successfull_if_only_one_record_is_returned()
        {
            // arrange
            var data = new List<TreeRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "12345", Title = "Test", All = "Test"}
            };
            connectionForId = connection = new InMemoryConnection(GetInMemoryData(data));
           
            InitializeElasticClient();
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
           
            // act
            var result = this.service.QueryForId<ElasticArchiveDbRecord>(12345, userAccess);

            // assert
            result.Response.Hits.Count.Should().Be(1);
            result.Response.Documents.Should().BeEquivalentTo(data);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Once);
        }

        [Test]
        public void Test_if_query_for_id_is__not_successfull_if_more_than_one_record_is_returned()
        {
            // arrange
            var data = new List<TreeRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "12345", Title = "Test", All = "Test"},
                new ElasticArchiveDbRecord {ArchiveRecordId = "12345", Title = "Haus am See", All = "Haus am See"}
            };

            connectionForId = connection = new InMemoryConnection(GetInMemoryData(data));

            InitializeElasticClient();
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
           
            // act
            Assert.Throws<ArgumentException>(() => { this.service.QueryForId<ElasticArchiveDbRecord>(12345, userAccess); }, "Query For Id must return exactly one record");
        }
        
        #endregion

        #region private Methods

        private byte[] GetInMemoryData(List<TreeRecord> records)
        {
            int length = records.Count;
            var list = new object[length];

            for(int index = 0; index < length; index++)
            {
                list[index] = RecordJsonStruct(records[index]);
            }
            
            var inMemoryData = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 1,
                    successful = 1,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = length },
                    max_score = length,
                    hits = list
                }
            };

            var json = JsonConvert.SerializeObject(inMemoryData);
            return Encoding.UTF8.GetBytes(json); 
        }

        private object RecordJsonStruct(TreeRecord record)
        {
           return new
            {
                _index = "archive",
                _type = "elasticArchiveDbRecord",
                _id = "archiveRecordId",
                _score = 1.0,
                _source = record
            };
        }

        private void InitializeElasticClient()
        {
            var node = new Uri("http://localhost:9200");
            string username = "(change here, but do not commit)";
            string pwd = "(change here,but do not commit)";
            var pool = new SingleNodeConnectionPool(node);
            var settings = new ConnectionSettings(pool, connection,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] { new ExpandoObjectConverter() }))
                .BasicAuthentication(username, pwd);

            settings.DisableDirectStreaming();
            settings.EnableDebugMode();
            settings.DisablePing();
            settings.DefaultIndex("default");
            settings.ThrowExceptions();
            var clientNormalSearch = new ElasticClient(settings);

            var settingsSearchForId = new ConnectionSettings(pool, connectionForId,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] { new ExpandoObjectConverter() }));

            var clientSearchForId = new ElasticClient(settingsSearchForId);

            clientProvider = new Mock<IElasticClientProvider>();
            clientProvider.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>())).Returns(clientNormalSearch);
            clientProvider.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>())).Returns(clientSearchForId);

            var translatorMock = new Mock<ITranslator>();

            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShort", It.IsAny<string>()))
                .Returns("search.termToShort");
            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShortForAll", It.IsAny<string>()))
                .Returns("search.termToShortForAll");
            var srb = new SearchRequestBuilder(new ElasticSettings(),
                new QueryTransformationService(ReadSearchSettings()), new List<TemplateField>());
            service = new ElasticService(clientProvider.Object, srb, new ElasticSettings(), new List<TemplateField>());
        }

        /// <summary>
        /// The query unfortunately has no influence on the search
        /// </summary>
        /// <returns>the Query</returns>
        private static ElasticQuery CreateElasticQuery()
        {
            var elasticQuery = new ElasticQuery();
            var boolQuery = new BoolQuery();
            var queries = new List<QueryContainer>
            {
                new QueryStringQuery
                {
                    Query = "Ball".Escape("title"),
                    DefaultField = "title",
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };

            boolQuery.Must = queries;
            elasticQuery.Query = boolQuery;
            return elasticQuery;
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

        #endregion

    }
}

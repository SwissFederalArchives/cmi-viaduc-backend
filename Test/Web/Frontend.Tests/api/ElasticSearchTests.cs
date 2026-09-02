using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Templates;
using CMI.Web.Frontend.API.Tests.ElasticMock;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Shouldly;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.API.Tests.api
{
    public class ElasticSearchTests
    {
        #region Fields
        private ElasticService service;
        private Mock<IElasticClientProvider> clientProvider;
        #endregion

        #region Tests

        [Test]
        public void If_result_does_not_contain_anonymized_records_no_search_for_record_details_is_issued()
        {
            // arrange
            var data = new List<ElasticArchiveRecord>
            {
                new (){ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball"},
                new (){ArchiveRecordId = "12346", Title = "Ball", All = "Hund"}
            };


            var response = CreateMockResponse(data);
            InitializeElasticClient(response);

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = this.service.RunQuery<ElasticArchiveRecord>(CreateElasticQuery(), userAccess).Result;
            
            //assert
            result.Response.Hits.Count.ShouldBe(2);
            result.Response.Documents.ShouldBe(data);
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
            var dataDb = new List<ElasticArchiveDbRecord>
            {
                // OE3 user has no rights for this record
                new ElasticArchiveDbRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleBAR }},
                // OE3 user has rights for this record
                new ElasticArchiveDbRecord {ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> { AccessRoles.RoleOe3, AccessRoles.RoleBAR, },
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Title"
                    }
                }
            };
            var data = new List<ElasticArchiveRecord>
            {
                // OE3 user has no rights for this record
                new ElasticArchiveDbRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
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
            
            var response = CreateMockResponse(dataDb);
            InitializeElasticClient(response, data, dataDb);
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = service.RunQuery<ElasticArchiveRecord>(CreateElasticQuery(), userAccess).Result;

            // assert
            result.Response.Hits.Count.ShouldBe(3);
            result.Response.Documents.ShouldBe(data);
            result.Data.Items.Last().Data.Title.ShouldBe("Unanonymized Title");
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()), Times.Exactly(2));
        }

        [Test]
        public void Test_if_user_with_BAR_role_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var dataDb = new List<ElasticArchiveDbRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Hund"
                    }
                },
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

            var data = new List<ElasticArchiveRecord>
            {
                new ElasticArchiveRecord
                {
                    ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR}
                },
                new () {ArchiveRecordId = "12353", Title = "Ball", All = "Hund", CustomFields = customFields},
                new ElasticArchiveRecord
                {
                    ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR, AccessRoles.RoleOe3}
                }
            };
            var response = CreateMockResponse(dataDb);
            InitializeElasticClient(response, data, dataDb);

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            
            // act
            var result = this.service.RunQuery<ElasticArchiveRecord>(CreateElasticQuery(), userAccess).Result;

            // assert
            result.Response.Hits.Count.ShouldBe(3);
            result.Response.Documents.ShouldBe(data);

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.First().Data.Title.ShouldBe("Unanonymized Hund");
            result.Data.Items.Last().Data.Title.ShouldBe("Unanonymized Title");
            result.Data.Items.Where(h => h.Data.IsAnonymized).Any(h => h.Data.Title.Equals("Unanonymized Title")).ShouldBeTrue();

            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()), Times.Exactly(2));
        }

        [Test]
        public void Test_if_user_with_OE3_role_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var dataDb = new List<ElasticArchiveDbRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
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
            var data = new List<ElasticArchiveRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR},
                    UnanonymizedFields = new()
                    {
                        Title = "Geheimer Titel"
                    }
                },
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "12395", Title = "Test", All = "Test", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {AccessRoles.RoleBAR, AccessRoles.RoleOe3},
                    UnanonymizedFields = new()
                    {
                        Title = "Geheimer Titel"
                    }
                }
            };
            var response = CreateMockResponse(dataDb);
            InitializeElasticClient(response, data, dataDb);


            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = service.RunQuery<ElasticArchiveDbRecord>(CreateElasticQuery(), userAccess).Result;

            // assert
            result.Response.Hits.Count.ShouldBe(2);
            result.Response.Documents.Select(c => c as ElasticArchiveRecord).ToList().ShouldBeEquivalentTo(data);
            result.Data.Items.All(h => h.Data.Title.Equals("Geheimer Titel")).ShouldBeFalse();

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.Last().Data.Title.ShouldBe("Geheimer Titel");
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()), Times.Once);
        }

        [Test]
        public void Test_if_user_with_Oe3_role_with_EB_fetches_the_details_only_for_the_records_he_is_authorized_and_that_the_unanonymized_title_is_returned()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");

            var dataDb = new List<ElasticArchiveDbRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
                    IsAnonymized = true, FieldAccessTokens = new List<string> {"EB_S31830999", AccessRoles.RoleBAR},
                    UnanonymizedFields = new()
                    {
                        Title = "Unanonymized Hund"
                    }
                },
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

            var data = new List<ElasticArchiveRecord>
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball", CustomFields = customFields,
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
            var response = CreateMockResponse(dataDb);
            InitializeElasticClient(response, data, dataDb);

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            
            // act
            var result = this.service.RunQuery<ElasticArchiveRecord>(CreateElasticQuery(), userAccess).Result;

            // assert
            result.Response.Hits.Count.ShouldBe(3);
            result.Response.Documents.ShouldBe(data);

            // Test if the unanonymized titles are returned
            // Because the detail query always returns the same record, we must test for the same title text
            result.Data.Items.First().Data.Title.ShouldBe("Unanonymized Hund"); 
            result.Data.Items.Last().Data.Title.ShouldBe("Unanonymized Title");

            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()), Times.Exactly(2));
        }

        [Test]
        public void Test_if_query_for_id_is_successfull_if_only_one_record_is_returned()
        {
            // arrange
            var dataDb = new List<ElasticArchiveDbRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Test", All = "Test"}
            };

            var data = new List<ElasticArchiveRecord>
            {
                new ElasticArchiveDbRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Test", All = "Test"}
            };
            var response = CreateMockResponse(dataDb);
            InitializeElasticClient(response, data, dataDb);

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
           
            // act
            var result = this.service.QueryForId<ElasticArchiveDbRecord>("Klas    2badb81d-ca89-5491-a0a2-78051750b341", userAccess).Result;

            // assert
            result.Response.Hits.Count.ShouldBe(1);
            result.Response.Documents.First().ShouldBeEquivalentTo(data[0]);
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Exactly(2));
        }

        #endregion

        #region private Methods


        private void InitializeElasticClient(SearchResponse<ElasticArchiveRecord> response)
        {
            var clientSearchForId  = new Mock<ElasticsearchClient>();

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
            service = new ElasticService(clientProvider.Object, srb.Object, new ElasticSettings(), new List<TemplateField>());

        }


        private void InitializeElasticClient(SearchResponse<ElasticArchiveDbRecord> response, List<ElasticArchiveRecord> data, List<ElasticArchiveDbRecord> dataDb)
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
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(CreateMockResponse(data)));
            clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            service = new ElasticServiceMock(clientProvider.Object, srb.Object, new ElasticSettings(), new List<TemplateField>() , dataDb);

        }

        /// <summary>
        /// The query unfortunately has no influence on the search
        /// </summary>
        /// <returns>the Query</returns>
        private static ElasticQuery CreateElasticQuery()
        {
            var elasticQuery = new ElasticQuery();
            var boolQuery = new BoolQuery();
            var queries = new List<Query>
            {
                new QueryStringQuery ("Ball".Escape("title"))
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

        private static SearchResponse<ElasticArchiveDbRecord> CreateMockResponse(List<ElasticArchiveDbRecord> list)
        {
            var hitList = new List<Hit<ElasticArchiveDbRecord>>();
            foreach (var item in list)
            {
                var hit = new Hit<ElasticArchiveDbRecord>(item.ArchiveRecordId, "test-index")
                {
                    Source = item
                };
                hitList.Add(hit);
            }
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

            return searchResponse;
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


        #endregion

    }
}

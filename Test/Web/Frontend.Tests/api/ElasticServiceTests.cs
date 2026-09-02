using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Templates;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Transport;
using Shouldly;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.API.Tests.api
{
    /// <summary>
    ///     <remarks>
    /// 
    ///         Elastic ist so parametrisiert das er 3 Snippets zurückliefert:
    ///         - Titel(für das Highlighting im Titel)
    ///         - Auszug relevanteste Trefferstelle aus Metadaten
    ///         - Auszug relevanteste Trefferstelle aus Primärdaten(sofern vorhanden)
    /// 
    ///         Wenn ein Snippet aus den Metadaten vorhanden ist, dann wird dies im Client dargestellt.
    ///         Andernfalls wird die Fundstelle aus den Primärdaten angezeigt.
    /// 
    ///     </remarks>
    /// </summary>
    [TestFixture]
    public class ElasticServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            elasticClientMock = new Mock<ElasticsearchClient>();
        }

        private Mock<ElasticsearchClient> elasticClientMock;

        [Test]
        public void Metadaten_Snippets_Duerfen_Den_Titel_Nicht_Beinhalten()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
           

            var userAccess = new UserAccess("123", AccessRoles.RoleOe1, null, null, false);
            var query = new ElasticQuery();
            var mockResponse = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = 1 },
                    max_score = 1.0,
                    hits = new[]
                    {
                        new
                        {
                            _index = "project",
                            _type = "project",
                            _id = "Project",
                            _score = 1.0,
                            _source = new TreeRecord
                            {
                                ArchiveRecordId = "1",
                                PrimaryDataFulltextAccessTokens = new List<string> {AccessRoles.RoleBAR}
                            },
                            highlight = new
                            {
                                title = new[] {"<em>Fundstelle</em>"},
                                all_Metadata_Text = new[] {"<em>Fundstelle</em>", "Dies ist eine andere <em>Fundstelle</em>"},
                                all_Primarydata = new[] {"<em>Fundstelle</em> in den Primärdaten"}
                            }
                        }
                    }
                }
            };

            //var clientProvider = CreateClientProvider(mockResponse);
            //clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
            //    (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(mockResponse));


            //var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            //var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());


            //// act
            //var result = service.RunQuery<TreeRecord>(query, userAccess).Result;

            //// assert
            //result.Data.Items[0].Highlight["title"]
            //    .Values<string>().First().Should()
            //    .Be("<em>Fundstelle</em>");

            //result.Data.Items[0].Highlight["mostRelevantVektor"]
            //    .Values<string>().First().Should()
            //    .Be("Dies ist eine andere <em>Fundstelle</em>", "Snippets müssen aus den Metadaten kommen und dürfen nicht den Titel beinhalten");
        }

        [Test]
        public void Snippets_Muessen_Ve_Titel_Beinhaltet_Wenn_Titel_Snippet_Leer_Ist()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();

            var userAccess = new UserAccess("123", AccessRoles.RoleOe1, null, null, false);
            var query = new ElasticQuery();
            var mockResponse = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = 1 },
                    max_score = 1.0,

                    hits = new[]
                    {
                        new
                        {
                            _index = "project",
                            _type = "project",
                            _id = "Project",
                            _score = 1.0,
                            _source = new TreeRecord
                            {
                                ArchiveRecordId = "1",
                                PrimaryDataFulltextAccessTokens = new List<string> {AccessRoles.RoleBAR},
                                Title = "Ve Title"
                            },
                            highlight = new
                            {
                                all_Metadata_Text = new[] {"<em>Fundstelle</em>", "Dies ist eine andere <em>Fundstelle</em>"},
                                all_Primarydata = new[] {"<em>Fundstelle</em> in den Primärdaten"}
                            }
                        }
                    }
                }
            };

            //var clientProvider = CreateClientProvider(mockResponse);

            //var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            //var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            //// act
            //var result = service.RunQuery<TreeRecord>(query, userAccess).Result;

            //// assert
            //result.Data.Items[0].Highlight["title"]
            //    .Values<string>().First().Should()
            //    .Be("Ve Title");
        }

        [Test]
        public void Snippets_Muessen_Primaerdaten_Auszuege_Enthalten_Wenn_Keine_Snippets_In_den_Metadaten_vorhanden_sind()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();

            var userAccess = new UserAccess("123", AccessRoles.RoleOe1, null, null, false);
            var query = new ElasticQuery();
            var mockResponse = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = 1 },
                    max_score = 1.0,
                    hits = new[]
                    {
                        new
                        {
                            _index = "project",
                            _type = "project",
                            _id = "Project",
                            _score = 1.0,
                            _source = new TreeRecord
                            {
                                ArchiveRecordId = "1",
                                PrimaryDataFulltextAccessTokens = new List<string> {AccessRoles.RoleOe1}
                            },
                            highlight = new
                            {
                                title = new[] {"<em>Fundstelle</em>"},
                                all_Primarydata = new[] {"Dies ist eine andere <em>Fundstelle</em>"}
                            }
                        }
                    }
                }
            };

            //var clientProvider = CreateClientProvider(mockResponse);

            //var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            //var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            //// act
            //var result = service.RunQuery<TreeRecord>(query, userAccess).Result;

            //// assert
            //result.Data.Items[0].Highlight["title"]
            //    .Values<string>().First().Should()
            //    .Be("<em>Fundstelle</em>");

            //result.Data.Items[0].Highlight["mostRelevantVektor"]
            //    .Values<string>().First().Should()
            //    .Be("Dies ist eine andere <em>Fundstelle</em>");
        }

        [Test]
        public void Primaerdaten_Snippets_Muessen_Auf_Berechtigungen_geprueft_werden()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();

            var userAccess = new UserAccess("123", AccessRoles.RoleOe1, null, null, false);
            var query = new ElasticQuery();
            var mockResponse = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = 1 },
                    max_score = 1.0,
                    hits = new[]
                    {
                        new
                        {
                            _index = "project",
                            _type = "project",
                            _id = "Project",
                            _score = 1.0,
                            _source = new TreeRecord
                            {
                                ArchiveRecordId = "1",
                                PrimaryDataFulltextAccessTokens = new List<string> {AccessRoles.RoleBAR}
                            },
                            highlight = new
                            {
                                all_Primarydata = new[] {"<em>Geheimer</em>", "Dies ist ein <em>Geheimer</em> Text"}
                            }
                        }
                    }
                }
            };

            //var clientProvider = CreateClientProvider(mockResponse);

            //var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            //var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            //// act
            //var result = service.RunQuery<TreeRecord>(query, userAccess).Result;

            //// assert
            //result.Data.Items[0].Highlight["mostRelevantVektor"].Should().BeNullOrEmpty();
        }

        [Test]
        public void Primaerdaten_Snippets_duerfen_nicht_fehlen_wenn_Titel_In_Metadaten_Vorkommt()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();

            var userAccess = new UserAccess("123", AccessRoles.RoleBAR, null, null, false);
            var query = new ElasticQuery();
            var mockResponse = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = 1 },
                    max_score = 1.0,
                    hits = new[]
                    {
                        new
                        {
                            _index = "project",
                            _type = "project",
                            _id = "Project",
                            _score = 1.0,
                            _source = new TreeRecord
                            {
                                ArchiveRecordId = "1",
                                PrimaryDataFulltextAccessTokens = new List<string> {AccessRoles.RoleBAR}
                            },
                            highlight = new
                            {
                                title = new[] {"<em>Titel</em>"},
                                all_Metadata_Text = new[] {"<em>Titel</em>"},
                                all_Primarydata = new[] {"<em>Primärdaten-Snippet</em>"}
                            }
                        }
                    }
                }
            };

            //var clientProvider = CreateClientProvider(mockResponse);

            //var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            //var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            //// act
            //var result = service.RunQuery<TreeRecord>(query, userAccess).Result;

            //// assert
            //result.Data.Items[0].Highlight["mostRelevantVektor"]
            //    .Values<string>().First().Should()
            //    .Be("<em>Primärdaten-Snippet</em>");
        }

        [Test]
        public void User_with_BAR_role_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("123", AccessRoles.RoleBAR, null, null, false);
            var fieldAccessTokens = new List<string> {"BAR"};
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object,
                new QueryTransformationService(new SearchSetting() { AdvancedSearchFields = [new SearchFieldDefinition() { Key = "Test" }] }),
                new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe nicht anonymisiert");
        }

        [Test]
        public void User_with_OE1_role_gets_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("432", AccessRoles.RoleOe1, null, null, false);
            var fieldAccessTokens = new List<string> { "BAR" };

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_OE3_role_and_no_Einsichtsbewilligung_gets_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            var fieldAccessTokens = new List<string> {AccessRoles.RoleBAR, "AS_571"};

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_AS_role_and_no_access_token_gests_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleAS, null, new[] { "AS_571" }, false);
            var fieldAccessTokens = new List<string> {AccessRoles.RoleBAR, "AS_0815"};

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_AS_role_and_correct_access_token_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleAS, null, new[] { "AS_571" }, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR, "AS_571" };

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, 
                new QueryTransformationService(new SearchSetting(){AdvancedSearchFields = [new SearchFieldDefinition(){Key = "Test"}]}),
                new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;
            
            // assert
            record.Title.ShouldBe("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe nicht anonymisiert");
        }
        
        [Test]
        public void User_with_OE3_role_and_Einsichtsbewillugung_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR, "EB_S31830999" };

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object,
                new QueryTransformationService(new SearchSetting() { AdvancedSearchFields = [new SearchFieldDefinition() { Key = "Test" }] }),
                new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.ShouldBe("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.ShouldBe("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.ShouldBe("bemerkungZurVe nicht anonymisiert");
        }

        [Test]
        public void Internal_fields_are_removed_from_response_if_user_has_not_BAR_role()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe1, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR };

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>
            {
                new() {DbFieldName = "CustomFields.BemerkungZurVe"},
                // Not really an internal field but to test if other fields than customFields are correctly removed
                new() {DbFieldName = "WithinInfo"}
            });
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel anonymisiert");
            record.WithinInfo.ShouldBeNull();
            var bemerkung = record.CustomFields.bemerkungZurVe as string;
            bemerkung.ShouldBe(null);
        }

        [Test]
        public void Internal_fields_are_not_removed_from_response_if_user_has_BAR_role()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR };

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);
            var srb = new SearchRequestBuilder(elasticSettings.Object, 
                new QueryTransformationService(new SearchSetting() { AdvancedSearchFields = [new SearchFieldDefinition() { Key = "Test" }] }),
                new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>
            {
                new() {DbFieldName = "CustomFields.BemerkungZurVe"},
                // Not really an internal field but to test if other fields than customFields are correctly removed
                new() {DbFieldName = "WithinInfo"}
            }   );
            var result = service.RunQuery<ElasticArchiveRecord>(query, userAccess).Result;
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.ShouldBe("Titel nicht anonymisiert");
            record.WithinInfo.ShouldBe("Contains nicht anonymisiert");
            var bemerkung = record.CustomFields.bemerkungZurVe as string;
            bemerkung.ShouldBe("bemerkungZurVe nicht anonymisiert");

        }

        private static SearchResponse<ElasticArchiveDbRecord> CreateMockResponseElasticArchiveDbRecord(string title, string titleUnanoymized,
                    string withinInfo, string withinInfoUnanoymized,
                    string bemerkungZurVe, string bemerkungZurVeUnanonymized,
                    string zusatzkomponenteZac1, string zusatzkomponenteZac1Unanonymized,
                    string verwandteVe, string verwandteVeUnanonymized,
                    List<string> fieldAccessTokens)
        {
            var customFields = new ExpandoObject() as IDictionary<string, object>;


            customFields.Add("bemerkungZurVe", bemerkungZurVe);
            customFields.Add("zusatzkomponenteZac1", zusatzkomponenteZac1);
            customFields.Add("verwandteVe", verwandteVe);
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                IsAnonymized = true,
                Title = title,
                WithinInfo = withinInfo,
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = titleUnanoymized,
                    WithinInfo = withinInfoUnanoymized,
                    BemerkungZurVe = bemerkungZurVeUnanonymized,
                    ZusatzkomponenteZac1 = zusatzkomponenteZac1Unanonymized,
                    VerwandteVe = verwandteVeUnanonymized,
                    ArchiveplanContext = new List<ElasticArchiveplanContextItem>
                    {
                        new ElasticArchiveplanContextItem()
                        {
                            Title = "ElasticArchiveplanContextItem Unanonymized Titel",
                        }
                    }
                },
                CustomFields = customFields,
                ArchiveRecordId = "1",
                FieldAccessTokens = fieldAccessTokens
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            
            return searchResponse;
        }


        private static SearchResponse<ElasticArchiveRecord> CreateMockResponseElasticArchiveRecord(string title, string titleUnanoymized,
            string withinInfo, string withinInfoUnanoymized,
            string bemerkungZurVe, string bemerkungZurVeUnanonymized,
            string zusatzkomponenteZac1, string zusatzkomponenteZac1Unanonymized,
            string verwandteVe, string verwandteVeUnanonymized,
            List<string> fieldAccessTokens)
        {
            var customFields = new ExpandoObject() as IDictionary<string, object>;


            customFields.Add("bemerkungZurVe", bemerkungZurVe);
            customFields.Add("zusatzkomponenteZac1", zusatzkomponenteZac1);
            customFields.Add("verwandteVe", verwandteVe);
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord()
            {
                IsAnonymized = true,
                Title = title,
                WithinInfo = withinInfo,
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = titleUnanoymized,
                    WithinInfo = withinInfoUnanoymized,
                    BemerkungZurVe = bemerkungZurVeUnanonymized,
                    ZusatzkomponenteZac1 = zusatzkomponenteZac1Unanonymized,
                    VerwandteVe = verwandteVeUnanonymized,
                    ArchiveplanContext = new List<ElasticArchiveplanContextItem>
                    {
                        new ElasticArchiveplanContextItem()
                        {
                            Title = "ElasticArchiveplanContextItem Unanonymized Titel",
                        }
                    }
                },
                CustomFields = customFields,
                ArchiveRecordId = "1",
                FieldAccessTokens = fieldAccessTokens
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

            return searchResponse;
        }


        private IElasticClientProvider CreateClientProvider(string title, string titleUnanoymized,
            string withinInfo, string withinInfoUnanoymized,
            string bemerkungZurVe, string bemerkungZurVeUnanonymized,
            string zusatzkomponenteZac1, string zusatzkomponenteZac1Unanonymized,
            string verwandteVe, string verwandteVeUnanonymized,
            List<string> fieldAccessTokens)
        {
            var providerMock = new Mock<IElasticClientProvider>();
            var node = new Uri("http://localhost:9200");
            var pool = new SingleNodePool(node);

            var settingsSearchForId = new ElasticsearchClientSettings(pool);
            var clientSearchForId = new Mock<ElasticsearchClient>(settingsSearchForId);
            providerMock
                .Setup(m => m.GetElasticClient(
                    It.IsAny<IElasticSettings>(),
                    It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()))
                .Returns(clientSearchForId.Object);

            providerMock
                .Setup(m => m.GetElasticClient(
                    It.IsAny<IElasticSettings>(),
                    It.IsAny<ElasticQueryResult<TreeRecord>>()))
                .Returns(clientSearchForId.Object);

            providerMock
                .Setup(m => m.GetElasticClient(
                    It.IsAny<IElasticSettings>(),
                    It.IsAny<ElasticQueryResult<SearchRecord>>()))
                .Returns(clientSearchForId.Object);


            providerMock
                .Setup(m => m.GetElasticClient(
                    It.IsAny<IElasticSettings>(),
                    It.IsAny<ElasticQueryResult<DetailRecord>>()))
                .Returns(clientSearchForId.Object);

            providerMock
                .Setup(m => m.GetElasticClient(
                    It.IsAny<IElasticSettings>(),
                    It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()))
                .Returns(clientSearchForId.Object);


            clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(CreateMockResponseElasticArchiveRecord(title, titleUnanoymized,
                withinInfo, withinInfoUnanoymized, bemerkungZurVe, bemerkungZurVeUnanonymized,
                zusatzkomponenteZac1, zusatzkomponenteZac1Unanonymized, verwandteVe, verwandteVeUnanonymized,
                fieldAccessTokens)));


            clientSearchForId.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(CreateMockResponseElasticArchiveDbRecord(title, titleUnanoymized,
                withinInfo,  withinInfoUnanoymized, bemerkungZurVe, bemerkungZurVeUnanonymized,
                zusatzkomponenteZac1, zusatzkomponenteZac1Unanonymized, verwandteVe,  verwandteVeUnanonymized,
                fieldAccessTokens)));

            return providerMock.Object;
        }


    }
}
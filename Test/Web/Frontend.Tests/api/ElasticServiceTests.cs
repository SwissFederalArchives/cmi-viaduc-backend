using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
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
using NUnit.Framework;

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

            var clientProvider = CreateClientProvider(mockResponse);

            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["title"]
                .Values<string>().First().Should()
                .Be("<em>Fundstelle</em>");

            result.Data.Items[0].Highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be("Dies ist eine andere <em>Fundstelle</em>", "Snippets müssen aus den Metadaten kommen und dürfen nicht den Titel beinhalten");
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

            var clientProvider = CreateClientProvider(mockResponse);

            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["title"]
                .Values<string>().First().Should()
                .Be("Ve Title");
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

            var clientProvider = CreateClientProvider(mockResponse);

            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["title"]
                .Values<string>().First().Should()
                .Be("<em>Fundstelle</em>");

            result.Data.Items[0].Highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be("Dies ist eine andere <em>Fundstelle</em>");
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

            var clientProvider = CreateClientProvider(mockResponse);

            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["mostRelevantVektor"].Should().BeNullOrEmpty();
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

            var clientProvider = CreateClientProvider(mockResponse);

            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be("<em>Primärdaten-Snippet</em>");
        }

        [Test]
        public void User_with_BAR_role_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("123", AccessRoles.RoleBAR, null, null, false);
            var fieldAccessTokens = new List<string> {"BAR"};
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe nicht anonymisiert");
        }

        [Test]
        public void User_with_OE1_role_gets_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("432", AccessRoles.RoleOe1, null, null, false);
            var fieldAccessTokens = new List<string> { "BAR" };
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_OE3_role_and_no_Einsichtsbewilligung_gets_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            var fieldAccessTokens = new List<string> {AccessRoles.RoleBAR, "AS_571"};
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_AS_role_and_no_access_token_gests_anonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleAS, null, new[] { "AS_571" }, false);
            var fieldAccessTokens = new List<string> {AccessRoles.RoleBAR, "AS_0815"};
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe anonymisiert");
        }

        [Test]
        public void User_with_AS_role_and_correct_access_token_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleAS, null, new[] { "AS_571" }, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR, "AS_571" };
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;
            
            // assert
            record.Title.Should().Be("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe nicht anonymisiert");
        }
        
        [Test]
        public void User_with_OE3_role_and_Einsichtsbewillugung_gets_unanonymized_Record()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR, "EB_S31830999" };
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>());
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel nicht anonymisiert");
            string test = record.CustomFields.verwandteVe;
            test.Should().Be("verwandteVe nicht anonymisiert");
            test = record.CustomFields.zusatzkomponenteZac1;
            test.Should().Be("Zusatzmerkmal nicht anonymisiert");
            test = record.CustomFields.bemerkungZurVe;
            test.Should().Be("bemerkungZurVe nicht anonymisiert");
        }

        [Test]
        public void Internal_fields_are_removed_from_response_if_user_has_not_BAR_role()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe1, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR };
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>
            {
                new() {DbFieldName = "CustomFields.BemerkungZurVe"},
                // Not really an internal field but to test if other fields than customFields are correctly removed
                new() {DbFieldName = "WithinInfo"}
            });
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel anonymisiert");
            record.WithinInfo.Should().BeNull();
            var bemerkung = record.CustomFields.bemerkungZurVe as string;
            bemerkung.Should().Be(null);
        }

        [Test]
        public void Internal_fields_are_not_removed_from_response_if_user_has_BAR_role()
        {
            // arrange
            var elasticSettings = new Mock<IElasticSettings>();
            elasticSettings.Setup(m => m.IdField).Returns("archiveRecordId");
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var fieldAccessTokens = new List<string> { AccessRoles.RoleBAR };
            var mockResponse = CreateMockResponse("Titel anonymisiert", "Titel nicht anonymisiert",
                "Contains anonymisiert", "Contains nicht anonymisiert",
                "bemerkungZurVe anonymisiert", "bemerkungZurVe nicht anonymisiert",
                "Zusatzmerkmal anonymisiert", "Zusatzmerkmal nicht anonymisiert",
                "verwandteVe anonymisiert", "verwandteVe nicht anonymisiert",
                fieldAccessTokens);

            // act
            var query = new ElasticQuery();
            var clientProvider = CreateClientProvider(mockResponse);
            var srb = new SearchRequestBuilder(elasticSettings.Object, new QueryTransformationService(null), new List<TemplateField>());
            var service = new ElasticService(clientProvider, srb, elasticSettings.Object, new List<TemplateField>
            {
                new() {DbFieldName = "CustomFields.BemerkungZurVe"},
                // Not really an internal field but to test if other fields than customFields are correctly removed
                new() {DbFieldName = "WithinInfo"}
            });
            var result = service.RunQuery<DetailRecord>(query, userAccess);
            var record = result.Data.Items[0].Data;

            // assert
            record.Title.Should().Be("Titel nicht anonymisiert");
            record.WithinInfo.Should().Be("Contains nicht anonymisiert");
            var bemerkung = record.CustomFields.bemerkungZurVe as string;
            bemerkung.Should().Be("bemerkungZurVe nicht anonymisiert");

        }

        private static object CreateMockResponse(string title, string titleUnanoymized,
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
                            _source = new ElasticArchiveDbRecord
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
                            }
                        }
                    }
                }
            };
            return mockResponse;
        }

        private IElasticClientProvider CreateClientProvider(object responseMock)
        {
            var providerMock = new Mock<IElasticClientProvider>();
            providerMock.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>())).Returns(
                () =>
                {
                    var response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseMock));
                    var connection = new InMemoryConnection(response);
                    var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
                    var settings = new ConnectionSettings(connectionPool, connection,
                        (serializer, values) => new JsonNetSerializer(
                            serializer, values, null, null,
                            new[] { new ExpandoObjectConverter() }));

                    return new ElasticClient(settings);
                });

            providerMock.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>())).Returns(
                () =>
                {
                    var response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseMock));
                    var connection = new InMemoryConnection(response);
                    var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
                    var settings = new ConnectionSettings(connectionPool, connection,
                        (serializer, values) => new JsonNetSerializer(
                            serializer, values, null, null,
                            new[] { new ExpandoObjectConverter() }));

                    return new ElasticClient(settings);
                });

            providerMock.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<DetailRecord>>())).Returns(
                () =>
                {
                    var response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseMock));
                    var connection = new InMemoryConnection(response);
                    var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
                    var settings = new ConnectionSettings(connectionPool, connection,
                        (serializer, values) => new JsonNetSerializer(
                            serializer, values, null, null,
                            new[] { new ExpandoObjectConverter() }));

                    return new ElasticClient(settings);
                });

            return providerMock.Object;
        }


    }
}
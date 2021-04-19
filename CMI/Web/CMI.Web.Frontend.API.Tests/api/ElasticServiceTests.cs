using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using Elasticsearch.Net;
using FluentAssertions;
using Moq;
using Nest;
using Newtonsoft.Json;
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
                    total = 1,
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

            var service = new ElasticService(clientProvider, elasticSettings.Object);

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
                    total = 1,
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

            var service = new ElasticService(clientProvider, elasticSettings.Object);

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
                    total = 1,
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

            var service = new ElasticService(clientProvider, elasticSettings.Object);

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
                    total = 1,
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

            var service = new ElasticService(clientProvider, elasticSettings.Object);

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
                    total = 1,
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

            var service = new ElasticService(clientProvider, elasticSettings.Object);

            // act
            var result = service.RunQuery<TreeRecord>(query, userAccess);

            // assert
            result.Data.Items[0].Highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be("<em>Primärdaten-Snippet</em>");
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
                    var settings = new ConnectionSettings(connectionPool, connection);
                    return new ElasticClient(settings);
                });

            return providerMock.Object;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Utilities.Cache.Access;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Transport;
using Shouldly;
using MassTransit;
using Moq;
using NUnit.Framework;
using CMI.Web.Frontend.api.Templates;


namespace CMI.Web.Frontend.API.Tests.Controller
{
    [TestFixture]
    public class FileControllerTests
    {
        [Test]
        public void GetOneTimeToken_For_A_Non_Existent_Ve_Should_Return_Forbidden()
        {
            // arrange
            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> {Items = new List<Entity<ElasticArchiveRecord>>()}
                }));


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, userDataAccessMock, null, null,
                null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.GetOneTimeToken("1").ConfigureAwait(false).GetAwaiter().GetResult();
            ;

            // assert
            result.ShouldBeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetOneTimeToken_For_A_Forbidden_Ve_Should_Return_Forbidden()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> {"BAR"},
                    PrimaryDataDownloadAccessTokens = new List<string> {"BAR"}
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
        
            var elasticService = InitializeElasticService(searchResponse);
            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());


            var sut = new FileController(null, null, null, null, null,
                elasticService, null,
                null, null, null, userDataAccessMock, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken("1").ConfigureAwait(false).GetAwaiter().GetResult();
            ;

            // assert
            result.ShouldBeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetOneTimeToken_For_An_Allowed_Ve_But_Download_Usage_Exceeded_Should_Return_PreconditionFailed()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "Ö2" }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

            var elasticService = InitializeElasticService(searchResponse);
            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());

            var usageResult = new Threshold?(new Threshold
            {
                IsEndingIn = TimeSpan.FromDays(365),
                UsageInterval = TimeSpan.FromHours(1),
                Usages = 100000000
            });

            var mockUsageAnalizer = new Mock<IUsageAnalyzer>();
            mockUsageAnalizer.Setup(m => m.GetExceededThreshold(It.IsAny<string>(), It.IsAny<HttpRequestMessage>()))
                .Returns(
                    usageResult);
            mockUsageAnalizer.Setup(m => m.GetText(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns("this is a usage text");

            var mockTranslator = Mock.Of<ITranslator>(s =>
                s.GetTranslation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()) == "translated text");

            var sut = new FileController(null, null, null, null, null, elasticService, mockUsageAnalizer.Object, null, mockTranslator, null,
                userDataAccessMock, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken("1").ConfigureAwait(false).GetAwaiter().GetResult();
            ;

            // assert
            result.ShouldBeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        }

        [Test]
        public void GetOneTimeToken_For_An_Allowed_Ve_Within_UsageThreshold_Should_Return_Valid_Token()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "Ö2" }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

            var elasticService = InitializeElasticService(searchResponse);
            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());

            var downloadHelperMock = Mock.Of<IDownloadLogHelper>(setup => setup.CreateLogToken() == "VALID TOKEN");
            var downloadTokenDataAccessMock = Mock.Of<IDownloadTokenDataAccess>();
            var downloadLogDataAccessMock = new Mock<IDownloadLogDataAccess>();

            var mockUsageAnalizer = new Mock<IUsageAnalyzer>();
            mockUsageAnalizer.Setup(m => m.GetExceededThreshold(It.IsAny<string>(), It.IsAny<HttpRequestMessage>()))
                .Returns((Threshold?) null);

            var mockTranslator = Mock.Of<ITranslator>(s =>
                s.GetTranslation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()) == "translated text");

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock, downloadLogDataAccessMock.Object, elasticService,
                mockUsageAnalizer.Object, null, mockTranslator, null, userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken("1").ConfigureAwait(false).GetAwaiter().GetResult();
            ;

            // assert
            result.ShouldBeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).Content.ShouldBe("VALID TOKEN");

            downloadLogDataAccessMock.Verify(s => s.LogTokenGeneration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockUsageAnalizer.Verify(s => s.UpdateUsageStatistic(It.IsAny<string>(), It.IsAny<HttpRequestMessage>(), It.Is((int val) => val == 1)));
        }

        [Test]
        public void GetAssetInfo_For_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> {Items = new List<Entity<ElasticArchiveRecord>>()}
                }));

            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.GetAssetInfo("1").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<NotFoundResult>();
        }

        [Test]
        public void GetAssetInfo_For_Ve_Without_PackageId_Should_Return_BadRequest()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "Ö2" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>()
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);

            var elasticService = InitializeElasticService(searchResponse);

            var sut = new FileController(null, null, null, null, null, elasticService, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.GetAssetInfo("1").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<BadRequestErrorMessageResult>();
        }


        [Test]
        public void GetAssetInfo_For_Ve_Without_Correct_Permissions_Should_Return_Forbidden()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    PrimaryDataLink = "a valid packageid",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var sut = new FileController(null, null, null, null, null, elasticService, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö1", null, null, false);

            // act
            var result = sut.GetAssetInfo("1").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetAssetInfo_For_Ve_With_Correct_Permissions_Should_Return_Valid_Status()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    PrimaryDataLink = "a valid packageid",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var statusClientMock = new Mock<IRequestClient<GetAssetStatusRequest>>();
            var response = new Mock<Response<GetAssetStatusResult>>();
            response.Setup(r => r.Message).Returns(new GetAssetStatusResult
            {
                Status = AssetDownloadStatus.RequiresPreparation
            });

            statusClientMock.Setup(m => m.GetResponse<GetAssetStatusResult>(It.IsAny<GetAssetStatusRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .ReturnsAsync(response.Object);

            var sut = new FileController(null, statusClientMock.Object, null, null, null, elasticService, null, null, null, cacheHelperMock, null,
                null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = sut.GetAssetInfo("1").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<OkNegotiatedContentResult<GetAssetStatusResult>>();
            ((OkNegotiatedContentResult<GetAssetStatusResult>) result).Content.Status.ShouldBe(AssetDownloadStatus.RequiresPreparation);
        }


        [Test]
        public async Task GetAssetInfo_With_Exception_In_StatusClient_Should_ReThrow_Exception_For_GlobalExceptionHandler()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryDataLink = "a valid packageid",
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var cacheHelperMock = Mock.Of<ICacheHelper>();

            var statusClientMock = new Mock<IRequestClient<GetAssetStatusRequest>>();
            statusClientMock.Setup(m =>
                    m.GetResponse<GetAssetStatusResult>(It.IsAny<GetAssetStatusRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Throws(new Exception("Error in StatusClient"));

            var sut = new FileController(null, statusClientMock.Object, null, null, null, elasticService, null, null, null, cacheHelperMock, null,
                null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = (Func<Task<IHttpActionResult>>) (async () => await sut.GetAssetInfo("1"));

            // assert
            var exception = await Should.ThrowAsync<Exception>(action);
            exception.Message.ShouldBe("Error in StatusClient");
        }

        [Test]
        public void PrepareAsset_With_Optional_Parameters_For_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> {Items = new List<Entity<ElasticArchiveRecord>>()}
                }));

            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.PrepareAsset("1", null, "").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<NotFoundResult>();
        }

        [Test]
        public void PrepareAsset_For_Ve_Without_PackageId_Should_Return_BadRequest()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "Ö2" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>()
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var sut = new FileController(null, null, null, null, null, elasticService, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.PrepareAsset("1", "", "").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<BadRequestErrorMessageResult>();
        }


        [Test]
        public void PrepareAsset_For_Ve_Without_Correct_Permissions_Should_Return_Forbidden()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryDataLink = "a valid packageid",
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var sut = new FileController(null, null, null, null, null, elasticService, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö1", null, null, false);

            // act
            var result = sut.PrepareAsset("1", "", "").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void PrepareAsset_For_Ve_With_Correct_Permissions_Should_Return_Valid_PrepareAssetResult()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryDataLink = "a valid packageid",
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var prepareClientMock = new Mock<IRequestClient<PrepareAssetRequest>>();
            var response = new Mock<Response<PrepareAssetResult>>();
            response.Setup(r => r.Message).Returns(new PrepareAssetResult
            {
                Status = AssetDownloadStatus.InPreparationQueue,
                InQueueSince = DateTime.Now
            });

            prepareClientMock.Setup(m => m.GetResponse<PrepareAssetResult>(It.IsAny<PrepareAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .ReturnsAsync(response.Object);

            var sut = new FileController(null, null, prepareClientMock.Object, null, null, elasticService, null, null, null, cacheHelperMock,
                null, null, null, null);

            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = sut.PrepareAsset("1", "", "").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<OkNegotiatedContentResult<PrepareAssetResult>>();
            ((OkNegotiatedContentResult<PrepareAssetResult>) result).Content.Status.ShouldBe(AssetDownloadStatus.InPreparationQueue);
        }

        [Test]
        public async Task PrepareAsset_With_Exception_In_PrepareClient_Should_ReThrow_Exception_For_GlobalExceptionHandler()
        {
            // arrange
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö1" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryDataLink = "a valid packageid",
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a valid packageid",
                            FileCount = 1
                        }
                    }
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var prepareClientMock = new Mock<IRequestClient<PrepareAssetRequest>>();
            prepareClientMock.Setup(m =>
                    m.GetResponse<PrepareAssetResult>(It.IsAny<PrepareAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Throws(new Exception("Error in PrepareClient"));

            var sut = new FileController(null, null, prepareClientMock.Object, null, null, elasticService, null, null, null, cacheHelperMock,
                null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = (Func<Task<IHttpActionResult>>) (async () => await sut.PrepareAsset("1", "http://thisisalink.com", "de"));

            // assert
            var exception = await Should.ThrowAsync<Exception>(action);
            exception.Message.ShouldBe("Error in PrepareClient");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("     ")]
        public void DownloadFile_Without_Token_Should_Return_Forbidden(string token)
        {
            // arrange
            var sut = new FileController(null, null, null, null, null, null, null, null, null, null, null, null, null, null);

            // act
            var result = sut.DownloadFile("1", token).ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            ((NegotiatedContentResult<string>) result).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void DownloadFile_Without_Valid_Token_Should_Return_BadRequest()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(false);

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, null, null, null, null, null, null, null,
                downloadHelperMock, null);

            // act
            var result = sut.DownloadFile("1", "invalid or old token").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<BadRequestErrorMessageResult>().Message.ShouldBe("Token expired or is not valid");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public void DownloadFile_With_A_Valid_Token_But_No_UserId_Should_Return_Forbidden(string userId)
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, null, null, null, null, null, null, null,
                downloadHelperMock, null);

            // act
            var result = sut.DownloadFile("1", "valid token").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<NegotiatedContentResult<string>>().StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void DownloadFile_With_A_Valid_Token_But_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> {Items = new List<Entity<ElasticArchiveRecord>>()}
                }));

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticServiceMock, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.DownloadFile("1", "valid token").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<NotFoundResult>();
        }

        [Test]
        public void DownloadFile_With_A_Valid_Token_And_Ve_Without_PrimaryData_Should_Return_BadRequest()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("Best    9c427a63-b945-524c-820a-c411451025d9", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "Ö2" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>()
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticService, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.DownloadFile("Best    9c427a63-b945-524c-820a-c411451025d9", "valid token").ConfigureAwait(false).GetAwaiter()
                .GetResult();

            // assert
            result.ShouldBeOfType<BadRequestErrorMessageResult>().Message.ShouldBe
                ("VE does not contain any primarydata and/or a valid packageid");
        }

        [Test]
        public void DownloadFile_With_A_Valid_Token_But_User_Has_No_Valid_PrimaryDownloadToken_Should_Return_Forbidden()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);
           
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a package id"
                        }
                    },
                    PrimaryDataLink = "a package id"
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticService, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.DownloadFile("1", "valid token").ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Test]
        public void DownloadFile_With_A_Valid_Token_And_Access_To_Ve_Should_Work_And_Log_To_History()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);
            
            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("Best    9c427a63-b945-524c-820a-c411451025d9", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                    ReferenceCode = "E3300C#1996/320#145*",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a package id"
                        }
                    },
                    PrimaryDataLink = "a package id"
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var userDataAccessMock = new Mock<IUserDataAccess>();
            var response = new Mock<Response<DownloadAssetResult>>();
            response.Setup(r => r.Message).Returns(new DownloadAssetResult());


            var downloadClientMock = Mock.Of<IRequestClient<DownloadAssetRequest>>(setup =>
                setup.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(response.Object));
            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);
            var kontrollstellenInformer = new Mock<IKontrollstellenInformer>();

            var sut = new FileController(downloadClientMock, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticService, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformer.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = sut.DownloadFile("Best    9c427a63-b945-524c-820a-c411451025d9", "valid token", 1).ConfigureAwait(false).GetAwaiter().GetResult();

            // assert
            result.ShouldBeOfType<ResponseMessageResult>().Response
                .Content.Headers.ContentType.MediaType.ShouldBe("application/octet-stream");

            userDataAccessMock.Verify(m =>
                m.StoreDownloadReasonInHistory(It.IsAny<ElasticArchiveRecord>(), It.IsAny<User>(), It.IsAny<UserAccess>(), 1));
            downloadLogDataAccess.Verify(m => m.LogVorgang("valid token", "Download"));
            kontrollstellenInformer.Verify(m => m.InformIfNecessary(It.IsAny<UserAccess>(), It.IsAny<IList<VeInfo>>()));
        }


        [Test]
        [TestCase("E3/300C#1996/320#145*", "E3-300C#1996-320#145_15821.zip")]
        [TestCase("J2.365-08#2021/77#?1?*", "J2.365-08#2021-77#-1-_15821.zip")]
        [TestCase("E::3\\-01#1982/1#1016", "E--3--01#1982-1#1016_15821.zip")]
        public void DownloadFile_Check_FileName_Without_InvalidFileNameChars(string referenceCode, string fileName)
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("Best    9c427a63-b945-524c-820a-c411451025d9", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    ReferenceCode = referenceCode,
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a package id"
                        }
                    },
                    PrimaryDataLink = "a package id"
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var userDataAccessMock = new Mock<IUserDataAccess>();
            var response = new Mock<Response<DownloadAssetResult>>();
            response.Setup(r => r.Message).Returns(new DownloadAssetResult());


            var downloadClientMock = Mock.Of<IRequestClient<DownloadAssetRequest>>(setup =>
                setup.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(response.Object));
            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);
            var kontrollstellenInformer = new Mock<IKontrollstellenInformer>();

            var sut = new FileController(downloadClientMock, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticService, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformer.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = sut.DownloadFile("15821", "valid token", 1).ConfigureAwait(false).GetAwaiter().GetResult() as ResponseMessageResult;
            // assert
            result.ShouldNotBeNull();
            result.Response.Content.Headers.ContentDisposition.FileName.ShouldBe(fileName);
        }



        [Test]
        [TestCase("E3/300C#1996/320#145*", "\"E3-300C#1996-320#145_Best    9c427a63-b945-524c-820a-c411451025d9.zip\"")]
        [TestCase("J2.365-08#2021/77#?1?*", "\"J2.365-08#2021-77#-1-_Best    9c427a63-b945-524c-820a-c411451025d9.zip\"")]
        [TestCase("E::3\\-01#1982/1#1016", "\"E--3--01#1982-1#1016_Best    9c427a63-b945-524c-820a-c411451025d9.zip\"")]
        public void DownloadFile_Check_FileName_Without_InvalidFileNameChars_With_NewAIS(string referenceCode, string fileName)
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("Best    9c427a63-b945-524c-820a-c411451025d9", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                    ReferenceCode = referenceCode,
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a package id"
                        }
                    },
                    PrimaryDataLink = "a package id"
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);
            var userDataAccessMock = new Mock<IUserDataAccess>();
            var response = new Mock<Response<DownloadAssetResult>>();
            response.Setup(r => r.Message).Returns(new DownloadAssetResult());


            var downloadClientMock = Mock.Of<IRequestClient<DownloadAssetRequest>>(setup =>
                setup.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(response.Object));
            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);
            var kontrollstellenInformer = new Mock<IKontrollstellenInformer>();

            var sut = new FileController(downloadClientMock, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticService, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformer.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = sut.DownloadFile("Best    9c427a63-b945-524c-820a-c411451025d9", "valid token", 1).ConfigureAwait(false).GetAwaiter().GetResult() as ResponseMessageResult;
            // assert
            result.ShouldNotBeNull();
            result.Response.Content.Headers.ContentDisposition.FileName.ShouldBe(fileName);
        }

        [Test]
        public async Task DownloadFile_With_An_Exception_In_DownloadClient_Should_ReThrow_For_GlobalExceptionHandler()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IDownloadLogHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);
          

            var hitList = new List<Hit<ElasticArchiveRecord>>();
            var hit = new Hit<ElasticArchiveRecord>("Best    9c427a63-b945-524c-820a-c411451025d9", "test-index")
            {
                Source = new ElasticArchiveRecord
                {
                    ArchiveRecordId = "1",
                    MetadataAccessTokens = new List<string> { "Ö2" },
                    PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                    PrimaryData = new List<ElasticArchiveRecordPackage>
                    {
                        new ElasticArchiveRecordPackage
                        {
                            PackageId = "a package id"
                        }
                    },
                    PrimaryDataLink = "a package id"
                }
            };
            hitList.Add(hit);
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(hitList)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var elasticService = InitializeElasticService(searchResponse);

            var kontrollstellenInformerMock = new Mock<IKontrollstellenInformer>();

            var userDataAccessMock = new Mock<IUserDataAccess>();
            var downloadClientMock = new Mock<IRequestClient<DownloadAssetRequest>>();
            downloadClientMock.Setup(m =>
                    m.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Throws(new Exception("Error in downloadClient"));

            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);

            var sut = new FileController(downloadClientMock.Object, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticService, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformerMock.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = new Func<Task<IHttpActionResult>>(() => sut.DownloadFile("1", "valid token", 1));

            // assert
            var exception = await Should.ThrowAsync<Exception>(action);
            exception.Message.ShouldBe("Error in downloadClient");
        }


        private ElasticService InitializeElasticService(SearchResponse<ElasticArchiveRecord> response)
        {
            var clientSearchForId = new Mock<ElasticsearchClient>();

            var clientProvider = new Mock<IElasticClientProvider>();
            clientProvider.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<TreeRecord>>())).Returns(clientSearchForId.Object);
            clientProvider.Setup(m =>
                    m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveRecord>>()))
                .Returns(clientSearchForId.Object);
            clientProvider.Setup(m =>
                    m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()))
                .Returns(clientSearchForId.Object);

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


            return new ElasticService(clientProvider.Object, srb.Object, new ElasticSettings(), new List<TemplateField>());

        }
    }

}

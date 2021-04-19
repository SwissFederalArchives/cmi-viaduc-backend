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
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.Helpers;
using FluentAssertions;
using MassTransit;
using Moq;
using Nest;
using NUnit.Framework;

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
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>());


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, userDataAccessMock, null, null,
                null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = sut.GetOneTimeToken(1);

            // assert
            result.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetOneTimeToken_For_A_Forbidden_Ve_Should_Return_Forbidden()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"BAR"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"}
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });

            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, userDataAccessMock, null, null,
                null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken(1);

            // assert
            result.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetOneTimeToken_For_An_Allowed_Ve_But_Download_Usage_Exceeded_Should_Return_PreconditionFailed()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"Ö2"}
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

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

            var sut = new FileController(null, null, null, null, null, elasticServiceMock, mockUsageAnalizer.Object, null, mockTranslator, null,
                userDataAccessMock, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken(1);

            // assert
            result.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }

        [Test]
        public void GetOneTimeToken_For_An_Allowed_Ve_Within_UsageThreshold_Should_Return_Valid_Token()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"Ö2"}
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });

            var userDataAccessMock = Mock.Of<IUserDataAccess>(setup => setup.GetUser(It.IsAny<string>()) == new User());
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var downloadHelperMock = Mock.Of<IFileDownloadHelper>(setup => setup.CreateDownloadToken() == "VALID TOKEN");
            var downloadTokenDataAccessMock = Mock.Of<IDownloadTokenDataAccess>();
            var downloadLogDataAccessMock = new Mock<IDownloadLogDataAccess>();

            var mockUsageAnalizer = new Mock<IUsageAnalyzer>();
            mockUsageAnalizer.Setup(m => m.GetExceededThreshold(It.IsAny<string>(), It.IsAny<HttpRequestMessage>()))
                .Returns((Threshold?) null);

            var mockTranslator = Mock.Of<ITranslator>(s =>
                s.GetTranslation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()) == "translated text");

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock, downloadLogDataAccessMock.Object, elasticServiceMock,
                mockUsageAnalizer.Object, null, mockTranslator, null, userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = sut.GetOneTimeToken(1);

            // assert
            result.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).Content.Should().Be("VALID TOKEN");

            downloadLogDataAccessMock.Verify(s => s.LogTokenGeneration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()));
            mockUsageAnalizer.Verify(s => s.UpdateUsageStatistic(It.IsAny<string>(), It.IsAny<HttpRequestMessage>(), It.Is((int val) => val == 1)));
        }

        [Test]
        public async Task GetAssetInfo_For_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>());

            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.GetAssetInfo(1);

            // assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task GetAssetInfo_For_Ve_Without_PackageId_Should_Return_BadRequest()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"Ö2"},
                PrimaryData = new List<ElasticArchiveRecordPackage>()
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.GetAssetInfo(1);

            // assert
            result.Should().BeOfType<BadRequestErrorMessageResult>();
        }


        [Test]
        public async Task GetAssetInfo_For_Ve_Without_Correct_Permissions_Should_Return_Forbidden()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö1", null, null, false);

            // act
            var result = await sut.GetAssetInfo(1);

            // assert
            result.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task GetAssetInfo_For_Ve_With_Correct_Permissions_Should_Return_Valid_Status()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var statusClientMock = new Mock<IRequestClient<GetAssetStatusRequest, GetAssetStatusResult>>();
            statusClientMock.Setup(m => m.Request(It.IsAny<GetAssetStatusRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    new GetAssetStatusResult
                    {
                        Status = AssetDownloadStatus.RequiresPreparation
                    });

            var sut = new FileController(null, statusClientMock.Object, null, null, null, elasticServiceMock, null, null, null, cacheHelperMock, null,
                null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = await sut.GetAssetInfo(1);

            // assert
            result.Should().BeOfType<OkNegotiatedContentResult<GetAssetStatusResult>>();
            ((OkNegotiatedContentResult<GetAssetStatusResult>) result).Content.Status.Should().Be(AssetDownloadStatus.RequiresPreparation);
        }


        [Test]
        public void GetAssetInfo_With_Exception_In_StatusClient_Should_ReThrow_Exception_For_GlobalExceptionHandler()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var statusClientMock = new Mock<IRequestClient<GetAssetStatusRequest, GetAssetStatusResult>>();
            statusClientMock.Setup(m => m.Request(It.IsAny<GetAssetStatusRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Error in StatusClient"));

            var sut = new FileController(null, statusClientMock.Object, null, null, null, elasticServiceMock, null, null, null, cacheHelperMock, null,
                null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = (Func<Task<IHttpActionResult>>) (async () => await sut.GetAssetInfo(1));

            // assert
            action.Should().Throw<Exception>("the global exception handler is used to avoid publish callstacks").WithMessage("Error in StatusClient");
        }

        [Test]
        public async Task PrepareAsset_With_Optional_Parameters_For_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>());

            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.PrepareAsset(1, null, "");

            // assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task PrepareAsset_For_Ve_Without_PackageId_Should_Return_BadRequest()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"Ö2"},
                PrimaryData = new List<ElasticArchiveRecordPackage>()
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.PrepareAsset(1, "", "");

            // assert
            result.Should().BeOfType<BadRequestErrorMessageResult>();
        }


        [Test]
        public async Task PrepareAsset_For_Ve_Without_Correct_Permissions_Should_Return_Forbidden()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var sut = new FileController(null, null, null, null, null, elasticServiceMock, null, null, null, null, null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "Ö1", null, null, false);

            // act
            var result = await sut.PrepareAsset(1, "", "");

            // assert
            result.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task PrepareAsset_For_Ve_With_Correct_Permissions_Should_Return_Valid_PrepareAssetResult()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });


            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var prepareClientMock = new Mock<IRequestClient<PrepareAssetRequest, PrepareAssetResult>>();
            prepareClientMock.Setup(m => m.Request(It.IsAny<PrepareAssetRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    new PrepareAssetResult
                    {
                        Status = AssetDownloadStatus.InPreparationQueue,
                        InQueueSince = DateTime.Now
                    });

            var sut = new FileController(null, null, prepareClientMock.Object, null, null, elasticServiceMock, null, null, null, cacheHelperMock,
                null, null, null, null);

            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = await sut.PrepareAsset(1, "", "");

            // assert
            result.Should().BeOfType<OkNegotiatedContentResult<PrepareAssetResult>>();
            ((OkNegotiatedContentResult<PrepareAssetResult>) result).Content.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
        }

        [Test]
        public void PrepareAsset_With_Exception_In_PrepareClient_Should_ReThrow_Exception_For_GlobalExceptionHandler()
        {
            // arrange
            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö1"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a valid packageid",
                        FileCount = 1
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var cacheHelperMock = Mock.Of<ICacheHelper>();
            var prepareClientMock = new Mock<IRequestClient<PrepareAssetRequest, PrepareAssetResult>>();
            prepareClientMock.Setup(m => m.Request(It.IsAny<PrepareAssetRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Error in PrepareClient"));

            var sut = new FileController(null, null, prepareClientMock.Object, null, null, elasticServiceMock, null, null, null, cacheHelperMock,
                null, null, null, null);
            sut.GetUserAccessFunc = userId => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = (Func<Task<IHttpActionResult>>) (async () => await sut.PrepareAsset(1, "http://thisisalink.com", "de"));

            // assert
            action.Should().Throw<Exception>("the global exception handler is used to avoid publish callstacks")
                .WithMessage("Error in PrepareClient");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("     ")]
        public async Task DownloadFile_Without_Token_Should_Return_Forbidden(string token)
        {
            // arrange
            var sut = new FileController(null, null, null, null, null, null, null, null, null, null, null, null, null, null);

            // act
            var result = await sut.DownloadFile(1, token);

            // assert
            ((NegotiatedContentResult<string>) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task DownloadFile_Without_Valid_Token_Should_Return_BadRequest()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(false);

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, null, null, null, null, null, null, null,
                downloadHelperMock, null);

            // act
            var result = await sut.DownloadFile(1, "invalid or old token");

            // assert
            result.Should().BeOfType<BadRequestErrorMessageResult>().Which.Message.Should()
                .Be("Token expired or is not valid");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public async Task DownloadFile_With_A_Valid_Token_But_No_UserId_Should_Return_Forbidden(string userId)
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, null, null, null, null, null, null, null,
                downloadHelperMock, null);

            // act
            var result = await sut.DownloadFile(1, "valid token");

            // assert
            result.Should().BeOfType<NegotiatedContentResult<string>>().Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task DownloadFile_With_A_Valid_Token_But_InExistent_Ve_Should_Return_NotFound()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>());

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticServiceMock, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.DownloadFile(1, "valid token");

            // assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task DownloadFile_With_A_Valid_Token_And_Ve_Without_PrimaryData_Should_Return_BadRequest()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"Ö2"},
                PrimaryData = new List<ElasticArchiveRecordPackage>()
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticServiceMock, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, null, null, null, false);

            // act
            var result = await sut.DownloadFile(1, "valid token");

            // assert
            result.Should().BeOfType<BadRequestErrorMessageResult>().Which.Message.Should()
                .Be("VE does not contain any primarydata and/or a valid packageid");
        }

        [Test]
        public async Task DownloadFile_With_A_Valid_Token_But_User_Has_No_Valid_PrimaryDownloadToken_Should_Return_Forbidden()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a package id"
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var userDataAccessMock = Mock.Of<IUserDataAccess>();

            var sut = new FileController(null, null, null, downloadTokenDataAccessMock.Object, null, elasticServiceMock, null, null, null, null,
                userDataAccessMock, null, downloadHelperMock, null);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "Ö2", null, null, false);

            // act
            var result = await sut.DownloadFile(1, "valid token");

            // assert
            result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task DownloadFile_With_A_Valid_Token_And_Access_To_Ve_Should_Work_And_Log_To_History()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a package id"
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var userDataAccessMock = new Mock<IUserDataAccess>();
            var downloadClientMock = Mock.Of<IRequestClient<DownloadAssetRequest, DownloadAssetResult>>(setup =>
                setup.Request(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>()) == Task.FromResult(new DownloadAssetResult()));
            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);
            var kontrollstellenInformer = new Mock<IKontrollstellenInformer>();

            var sut = new FileController(downloadClientMock, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticServiceMock, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformer.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var result = await sut.DownloadFile(1, "valid token", 1);

            // assert
            result.Should().BeOfType<ResponseMessageResult>().Subject.Response
                .Content.Headers.ContentType.MediaType.Should().Be("application/octet-stream");

            userDataAccessMock.Verify(m =>
                m.StoreDownloadReasonInHistory(It.IsAny<ElasticArchiveRecord>(), It.IsAny<User>(), It.IsAny<UserAccess>(), 1));
            downloadLogDataAccess.Verify(m => m.LogVorgang("valid token", "Download"));
            kontrollstellenInformer.Verify(m => m.InformIfNecessary(It.IsAny<UserAccess>(), It.IsAny<IList<VeInfo>>()));
        }

        [Test]
        public void DownloadFile_With_An_Exception_In_DownloadClient_Should_ReThrow_For_GlobalExceptionHandler()
        {
            // arrange
            var downloadHelperMock = Mock.Of<IFileDownloadHelper>();
            var downloadTokenDataAccessMock = new Mock<IDownloadTokenDataAccess>();
            var downloadLogDataAccess = new Mock<IDownloadLogDataAccess>();

            downloadTokenDataAccessMock.Setup(m => m.CheckTokenIsValidAndClean(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<DownloadTokenType>(),
                    It.IsAny<string>()))
                .Returns(true);

            var userId = "a user id";
            downloadTokenDataAccessMock.Setup(m =>
                    m.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DownloadTokenType>(), It.IsAny<string>()))
                .Returns(userId);

            var mockHit = new Mock<IHit<ElasticArchiveRecord>>();
            mockHit.SetupGet(m => m.Source).Returns(new ElasticArchiveRecord
            {
                ArchiveRecordId = "1",
                MetadataAccessTokens = new List<string> {"Ö2"},
                PrimaryDataDownloadAccessTokens = new List<string> {"BAR"},
                PrimaryData = new List<ElasticArchiveRecordPackage>
                {
                    new ElasticArchiveRecordPackage
                    {
                        PackageId = "a package id"
                    }
                }
            });

            var mockElasticResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockElasticResponse.SetupGet(m => m.Hits).Returns(new List<IHit<ElasticArchiveRecord>>
            {
                mockHit.Object
            });
            var elasticServiceMock = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<int>(), It.IsAny<UserAccess>()) ==
                new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Response = mockElasticResponse.Object
                });

            var kontrollstellenInformerMock = new Mock<IKontrollstellenInformer>();

            var userDataAccessMock = new Mock<IUserDataAccess>();
            var downloadClientMock = new Mock<IRequestClient<DownloadAssetRequest, DownloadAssetResult>>();
            downloadClientMock.Setup(m => m.Request(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Error in downloadClient"));

            var cacheHelperMock = Mock.Of<ICacheHelper>(setup => setup.GetStreamFromCache(It.IsAny<string>()) == Stream.Null);

            var sut = new FileController(downloadClientMock.Object, null, null, downloadTokenDataAccessMock.Object, downloadLogDataAccess.Object,
                elasticServiceMock, null, null, null, cacheHelperMock, userDataAccessMock.Object, null, downloadHelperMock,
                kontrollstellenInformerMock.Object);
            sut.GetUserAccessFunc = uid => new UserAccess(userId, "BAR", null, null, false);

            // act
            var action = new Func<Task<IHttpActionResult>>(() => sut.DownloadFile(1, "valid token", 1));

            // assert
            action.Should().Throw<Exception>().WithMessage("Error in downloadClient");
        }
    }
}
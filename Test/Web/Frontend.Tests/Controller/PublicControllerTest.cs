using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.ParameterSettings;
using Shouldly;
using MassTransit;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.Controller
{
    [TestFixture]
    public class PublicControllerTests
    {
        private Mock<IElasticService> elasticMock;
        private Mock<IRequestClient<IGetSecurityTokens>> tokenClientMock;
        private PublicController controller;

        [SetUp]
        public void Setup()
        {
            var resourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            // Patch settings manually
            var settings = new NameValueCollection
    {
        { "configDirectory", resourceDir },
        { "clientConfigDirectory", resourceDir },
        { "clientDefaultPath", "/client/" },
        { "staticDefaultPath", "/content/" },
        { "staticPagePath", "/content/" },
        { "indexPagePath", "/client/index.html" },
        { "synonymMaxInputWords", "5" }
    };

            typeof(ServiceHelper)
                .GetField("settings", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, settings);

            var dirHelperMock = new Mock<IDirectoryHelper>();
            dirHelperMock.Setup(x => x.ConfigDirectory).Returns(resourceDir + Path.DirectorySeparatorChar);
            dirHelperMock.Setup(x => x.ClientConfigDirectory).Returns(resourceDir);
            dirHelperMock.Setup(x => x.ClientDefaultPath).Returns("/client/");
            dirHelperMock.Setup(x => x.StaticDefaultPath).Returns("/content/");
            dirHelperMock.Setup(x => x.StaticPagePath).Returns("/content/");
            dirHelperMock.Setup(x => x.IndexPagePath).Returns("/client/index.html");

            typeof(DirectoryHelper)
                .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, dirHelperMock.Object);

            // Mocks
            elasticMock = new Mock<IElasticService>();
            tokenClientMock = new Mock<IRequestClient<IGetSecurityTokens>>();

            controller = new PublicController(
                new Mock<IEntityProvider>().Object,
                new Mock<IModelData>().Object,
                new ManagementClientSettings(),
                new FrontendDynamicTextSettings
                {
                    DeliveryTypeDigitalDE = "Digital",
                    DeliveryTypeReadingRoomDE = "Lesesaal",
                    DeliveryTypeCommissionDE = "Kommission"
                },
                elasticMock.Object,
                tokenClientMock.Object
            );
        }


        [Test]
        public async Task GetAccessTokens_InvalidId_ShouldReturnEmptyTokens_NoError()
        {
            tokenClientMock.Setup(x =>
                x.GetResponse<GetSecurityTokensResponse>(It.IsAny<object>(), default, default))
                .ThrowsAsync(new Exception("Simulated failure"));

            elasticMock.Setup(x => x.QueryTokensForId(It.IsAny<string>()))
                       .Returns(Task.FromResult((AccessTokens)null));

            var result = await controller.GetAccessTokens("invalid-id") as JsonResult<JObject>;
            var response = result.Content.ToObject<AccessTokenCheckResult>();

            response.Calculated.ShouldNotBeNull();
            response.Elastic.ShouldNotBeNull();
            response.CheckError.ShouldBeFalse();
        }

        [Test]
        public async Task GetAccessTokens_ValidId_MatchingTokens_NoError()
        {
            var tokens = new AccessTokens
            {
                MetadataAccessTokens = "BAR",
                FulltextAccessTokens = "Ö3",
                DownloadAccessTokens = "Ö2",
                FieldAccessTokens = "BAR"
            };

            var mockResponse = Mock.Of<Response<GetSecurityTokensResponse>>(r =>
                r.Message == new GetSecurityTokensResponse { Calculated = tokens });

            tokenClientMock.Setup(x =>
                x.GetResponse<GetSecurityTokensResponse>(It.IsAny<object>(), default, default))
                .ReturnsAsync(mockResponse);

            elasticMock.Setup(x => x.QueryTokensForId(It.IsAny<string>()))
                       .Returns(Task.FromResult(tokens));

            var result = await controller.GetAccessTokens("valid-id") as JsonResult<JObject>;
            var response = result.Content.ToObject<AccessTokenCheckResult>();

            response.CheckError.ShouldBeFalse();
        }

        [Test]
        public async Task GetAccessTokens_ValidId_TokensDoNotMatch_ShouldReturnError()
        {
            var calculated = new AccessTokens
            {
                MetadataAccessTokens = "BAR",
                FulltextAccessTokens = "BAR",
                DownloadAccessTokens = "BAR",
                FieldAccessTokens = "BAR"
            };

            var elastic = new AccessTokens
            {
                MetadataAccessTokens = "Ö3",
                FulltextAccessTokens = "Ö3",
                DownloadAccessTokens = "Ö3",
                FieldAccessTokens = "Ö3"
            };

            var mockResponse = Mock.Of<Response<GetSecurityTokensResponse>>(r =>
                r.Message == new GetSecurityTokensResponse { Calculated = calculated });

            tokenClientMock.Setup(x =>
                x.GetResponse<GetSecurityTokensResponse>(It.IsAny<object>(), default, default))
                .ReturnsAsync(mockResponse);

            elasticMock.Setup(x => x.QueryTokensForId(It.IsAny<string>()))
                       .Returns(Task.FromResult(elastic));

            var result = await controller.GetAccessTokens("mismatch-id") as JsonResult<JObject>;
            var response = result.Content.ToObject<AccessTokenCheckResult>();

            response.CheckError.ShouldBeTrue();
        }
    }
}

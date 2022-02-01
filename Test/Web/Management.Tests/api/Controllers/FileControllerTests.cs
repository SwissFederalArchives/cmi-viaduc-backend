using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Utilities.Cache.Access;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Controllers;
using FluentAssertions;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Management.Tests.api.Controllers
{
    [TestFixture]
    public class FileControllerTests
    {
        [SetUp]
        public void SetupDefaultMocks()
        {
            orderManagerClient = Mock.Of<IPublicOrder>();
            downloadTokenDataAccess = Mock.Of<IDownloadTokenDataAccess>();
            downloadHelper = Mock.Of<IFileDownloadHelper>(c => c.GetClientIp(It.IsAny<HttpRequestMessage>()) == "0.0.0.0:127");
            var downloadAssetResponse = new Mock<Response<DownloadAssetResult>>();
            downloadAssetResponse.Setup(r => r.Message).Returns(new DownloadAssetResult
            {
                AssetDownloadLink = "mydownloadLink"
            });

            downloadClient = Mock.Of<IRequestClient<DownloadAssetRequest>>(
                c => c.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(downloadAssetResponse.Object));

            var downloadAssetNoLinkResponse = new Mock<Response<DownloadAssetResult>>();
            downloadAssetNoLinkResponse.Setup(r => r.Message).Returns(new DownloadAssetResult
            {
                AssetDownloadLink = ""
            });

            downloadClientNoLink = Mock.Of<IRequestClient<DownloadAssetRequest>>(
                c => c.GetResponse<DownloadAssetResult>(It.IsAny<DownloadAssetRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(downloadAssetNoLinkResponse.Object));

            cacheHelper = Mock.Of<ICacheHelper>(c =>
                c.GetStreamFromCache(It.IsAny<string>()) == (Stream) new MemoryStream(Encoding.UTF8.GetBytes(" a test")));

            var doesExistInCacheResponse = new Mock<Response<DoesExistInCacheResponse>>();
            doesExistInCacheResponse.Setup(r => r.Message).Returns(new DoesExistInCacheResponse
            {
                Exists = true,
                FileSizeInBytes = 100
            });

            doesExistInCacheClient = Mock.Of<IRequestClient<DoesExistInCacheRequest>>(c =>
                c.GetResponse<DoesExistInCacheResponse>(It.IsAny<DoesExistInCacheRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()) == Task.FromResult(doesExistInCacheResponse.Object));
        }

        private IPublicOrder orderManagerClient;
        private IDownloadTokenDataAccess downloadTokenDataAccess;
        private IFileDownloadHelper downloadHelper;
        private IRequestClient<DownloadAssetRequest> downloadClient;
        private ICacheHelper cacheHelper;
        private IRequestClient<DownloadAssetRequest> downloadClientNoLink;
        private IRequestClient<DoesExistInCacheRequest> doesExistInCacheClient;

        [Test]
        public void Invalid_token_returns_Forbidden()
        {
            var token = "  "; // Invalid token
            var recordId = 1;
            var fileController = new FileController(orderManagerClient, downloadTokenDataAccess, downloadHelper, downloadClient,
                doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            result.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void If_token_is_not_valid_returns_BadRequest()
        {
            var token = "myToken";
            var recordId = 1;
            var dtm = Mock.Of<IDownloadTokenDataAccess>(c =>
                c.CheckTokenIsValidAndClean(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) == false);

            var fileController = new FileController(orderManagerClient, dtm, downloadHelper, downloadClient, doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            result.Should().BeOfType<BadRequestErrorMessageResult>();
        }

        [Test]
        public void If_user_is_not_found_returns_Forbidden()
        {
            var token = "myToken";
            var recordId = 1;
            var dtm = Mock.Of<IDownloadTokenDataAccess>(c =>
                c.CheckTokenIsValidAndClean(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) &&
                c.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) == "");

            var fileController = new FileController(orderManagerClient, dtm, downloadHelper, downloadClient, doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            result.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) result).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void If_order_is_not_found_return_BadRequest()
        {
            var token = "myToken";
            var recordId = 1;
            var dtm = Mock.Of<IDownloadTokenDataAccess>(c =>
                c.CheckTokenIsValidAndClean(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) &&
                c.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) == "111");
            var omc = Mock.Of<IPublicOrder>(c => c.FindOrderItems(It.IsAny<int[]>()) == Task.FromResult(new OrderItem[0]));
            var fileController = new FileController(omc, dtm, downloadHelper, downloadClient, doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            result.Should().BeOfType<BadRequestErrorMessageResult>();
        }

        [Test]
        public void If_zip_file_is_not_found_in_cache_then_return_gone()
        {
            var token = "myToken";
            var recordId = 1;
            var dtm = Mock.Of<IDownloadTokenDataAccess>(c =>
                c.CheckTokenIsValidAndClean(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) &&
                c.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) == "111");
            var omc = Mock.Of<IPublicOrder>(c =>
                c.FindOrderItems(It.IsAny<int[]>()) == Task.FromResult(new[] {new OrderItem {Id = 100, Benutzungskopie = true}}));
            var fileController = new FileController(omc, dtm, downloadHelper, downloadClientNoLink, doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            result.Should().BeOfType<ResponseMessageResult>();
            ((ResponseMessageResult) result).Response.StatusCode.Should().Be(HttpStatusCode.Gone);
        }

        [Test]
        public void If_all_conditions_are_met_tokens_is_cleaned_up_and_result_is_as_expexted()
        {
            var token = "myToken";
            var recordId = 1;
            var dtm = Mock.Of<IDownloadTokenDataAccess>(c =>
                c.CheckTokenIsValidAndClean(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) &&
                c.GetUserIdByToken(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem, It.IsAny<string>()) == "111");
            var omc = Mock.Of<IPublicOrder>(c =>
                c.FindOrderItems(It.IsAny<int[]>()) == Task.FromResult(new[] {new OrderItem {Id = 100, Benutzungskopie = true}}));
            var fileController = new FileController(omc, dtm, downloadHelper, downloadClient, doesExistInCacheClient, cacheHelper);

            var result = fileController.DownloadFile(recordId, token).GetAwaiter().GetResult();

            // Assert
            Mock.Get(dtm).Verify(m => m.CleanUpOldToken(It.IsAny<string>(), It.IsAny<int>(), DownloadTokenType.OrderItem), Times.Once);
            result.Should().BeOfType<ResponseMessageResult>();
            var response = result.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            response.Content.Headers.ContentType.Should().Be(MediaTypeHeaderValue.Parse("application/octet-stream"));
            response.Content.Headers.ContentDisposition.FileName.Should().Be($"{recordId}.zip");
        }
    }
}
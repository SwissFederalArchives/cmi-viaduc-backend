using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Cache;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class GetAssetStatusConsumerTests
    {
        private Mock<IAssetManager> assetManager;
        private ITestHarness harness;
        private ServiceProvider provider;
        private Mock<IRequestClient<DoesExistInCacheRequest>> requestClient;
        private Mock<Response<DoesExistInCacheResponse>> doesExistInCacheResponseMock;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            requestClient = new Mock<IRequestClient<DoesExistInCacheRequest>>();
            doesExistInCacheResponseMock= new Mock<Response<DoesExistInCacheResponse>>();

            provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<DoesExistInCacheRequestConsumer>().Endpoint(e => e.Name = BusConstants.CacheDoesExistRequestQueue);
                    cfg.AddConsumer<GetAssetStatusConsumer>();
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => requestClient.Object);
                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>();
        }

        [Test]
        public async Task Status_InCache_is_returned_if_item_is_in_cache()
        {
            // Arrange
            doesExistInCacheResponseMock.SetupGet(x => x.Message).Returns(new DoesExistInCacheResponse
            {
                Exists = true
            });

            requestClient.Setup(r =>
                r.GetResponse<DoesExistInCacheResponse>(It.IsAny<DoesExistInCacheRequest>(), new CancellationToken(), new RequestTimeout())).ReturnsAsync(doesExistInCacheResponseMock.Object);
            assetManager.Setup(e => e.CheckPreparationStatus("1111"))
                .Returns(Task.FromResult(new PreparationStatus { PackageIsInPreparationQueue = false }));

            await harness.Start();

            var client = harness.GetRequestClient<GetAssetStatusRequest>();

            // Act
            var result = await client.GetResponse<GetAssetStatusResult>(new GetAssetStatusRequest
            {
                ArchiveRecordId = "1111",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "2"
            });

            
            // Assert
            Assert.IsTrue(await harness.Consumed.Any<GetAssetStatusRequest>());
            result.Message.Status.Should().Be(AssetDownloadStatus.InCache);
            await harness.Stop();
        }


        [Test]
        public async Task Status_InPreparationQueue_is_returned_if_item_is_in_queue()
        {
            // Arrange
            await harness.Start();

            doesExistInCacheResponseMock.SetupGet(x => x.Message).Returns(new DoesExistInCacheResponse
            {
                Exists = false
            });
            requestClient.Setup(r =>
                r.GetResponse<DoesExistInCacheResponse>(It.IsAny<DoesExistInCacheRequest>(), new CancellationToken(), new RequestTimeout())).ReturnsAsync(doesExistInCacheResponseMock.Object);
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus { PackageIsInPreparationQueue = true }));
            var client = harness.GetRequestClient<GetAssetStatusRequest>();
            // Act
            var result = await client.GetResponse<GetAssetStatusResult>(new GetAssetStatusRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "1"
            });
            // Assert
            Assert.IsTrue(await harness.Consumed.Any<GetAssetStatusRequest>());
            result.Message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
            await harness.Stop();
        }


        [Test]
        public async Task Status_RequiresPreparation_is_returned_neither_in_cache_nor_queue()
        {
            // Arrange
            await harness.Start();
            doesExistInCacheResponseMock = new Mock<Response<DoesExistInCacheResponse>>();
            doesExistInCacheResponseMock.SetupGet(x => x.Message).Returns(new DoesExistInCacheResponse
            {
                Exists = false
            });
            requestClient.Setup(r =>
                r.GetResponse<DoesExistInCacheResponse>(It.IsAny<DoesExistInCacheRequest>(), new CancellationToken(), new RequestTimeout())).ReturnsAsync(doesExistInCacheResponseMock.Object);

          
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus { PackageIsInPreparationQueue = false }));
            var client = harness.GetRequestClient<GetAssetStatusRequest>();



            // Act
            var result = await client.GetResponse<GetAssetStatusResult>(new GetAssetStatusRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "3"
            });

            // Assert
            result.Message.Status.Should().Be(AssetDownloadStatus.RequiresPreparation);
        }

    }
}
using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Cache;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class GetAssetStatusConsumerTests : InMemoryTestFixture
    {
        [SetUp]
        public void Setup()
        {
            requestClient = CreateRequestClient<GetAssetStatusRequest, GetAssetStatusResult>();
            doesExistsClient = CreateRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>();
            assetManager.Reset();
        }

        public GetAssetStatusConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly DoesExistInCacheRequestConsumer doesExistInCacheConsumer = new DoesExistInCacheRequestConsumer();
        private Task<ConsumeContext<GetAssetStatusRequest>> getAssetStatusTask;
        private IRequestClient<GetAssetStatusRequest, GetAssetStatusResult> requestClient;
        private IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistsClient;
        private Task<GetAssetStatusResult> response;


        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            getAssetStatusTask = Handler<GetAssetStatusRequest>(configurator,
                context => new GetAssetStatusConsumer(assetManager.Object, doesExistsClient).Consume(context));

            Handler<DoesExistInCacheRequest>(configurator, context => doesExistInCacheConsumer.Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
        }


        [Test]
        public async Task Status_InCache_is_returned_if_item_is_in_cache()
        {
            // Arrange
            assetManager.Setup(e => e.CheckPreparationStatus("1111"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = false}));

            doesExistInCacheConsumer.DoesExistFunc = context => new Tuple<bool, long>(true, 99999);

            // Act
            response = requestClient.Request(new GetAssetStatusRequest
            {
                ArchiveRecordId = "1111",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "2"
            });

            // Wait for the results
            var message = await response;
            await getAssetStatusTask;

            // Assert
            message.Status.Should().Be(AssetDownloadStatus.InCache);
            message.InQueueSince.Should().Be(DateTime.MinValue);
        }


        [Test]
        public async Task Status_InPreparationQueue_is_returned_if_item_is_in_queue()
        {
            // Arrange
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = true}));

            doesExistInCacheConsumer.DoesExistFunc = context => new Tuple<bool, long>(false, 0);

            // Act
            response = requestClient.Request(new GetAssetStatusRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "1"
            });

            // Wait for the results
            var message = await response;
            await getAssetStatusTask;

            // Assert
            message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
        }

        [Test]
        public async Task Status_RequiresPreparation_is_returned_neither_in_cache_nor_queue()
        {
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = false}));
            doesExistInCacheConsumer.DoesExistFunc = context => new Tuple<bool, long>(false, 0);

            // Act
            response = requestClient.Request(new GetAssetStatusRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "3"
            });

            // Wait for the results
            var message = await response;
            await getAssetStatusTask;

            // Assert
            message.Status.Should().Be(AssetDownloadStatus.RequiresPreparation);
        }
    }
}
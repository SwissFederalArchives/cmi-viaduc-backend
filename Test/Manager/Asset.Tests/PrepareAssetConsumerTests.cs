using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Index;
using CMI.Manager.Index.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PrepareAssetConsumerTests : InMemoryTestFixture
    {
        [SetUp]
        public void Setup()
        {
            requestClient = CreateRequestClient<PrepareAssetRequest, PrepareAssetResult>();
            indexClient = CreateRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>();
            assetManager.Reset();
            indexManagerConsumer.Reset();
        }

        public PrepareAssetConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<IIndexManager>> indexManagerConsumer = new Mock<IConsumer<IIndexManager>>();
        private Task<ConsumeContext<PrepareAssetRequest>> prepareAssetTask;
        private Task<ConsumeContext<FindArchiveRecordRequest>> findArchiveRecordTask;
        private IRequestClient<PrepareAssetRequest, PrepareAssetResult> requestClient;
        private IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> indexClient;
        private Task<PrepareAssetResult> response;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            prepareAssetTask = Handler<PrepareAssetRequest>(configurator,
                context => new PrepareAssetConsumer(assetManager.Object, Bus, indexClient).Consume(context));
            findArchiveRecordTask = Handler<FindArchiveRecordRequest>(configurator,
                context => new FindArchiveRecordConsumer(indexManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
        }

        [Test]
        public async Task DownloadPackage_message_is_not_sent_if_already_in_preparation()
        {
            // Arrange
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = true}));

            // Act
            response = requestClient.Request(new PrepareAssetRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "6"
            });

            // Wait for the results
            var message = await response;
            await prepareAssetTask;

            // Assert
            message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
        }

        [Test]
        public async Task Register_in_job_database_called_when_not_in_preperation_queue()
        {
            // Arrange
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = false}));
            assetManager.Setup(e => e.RegisterJobInPreparationQueue("999", "usuallySomeGuid", AufbereitungsArtEnum.Download,
                AufbereitungsServices.AssetService,
                It.IsAny<List<ElasticArchiveRecordPackage>>(), It.IsAny<object>())).Returns(() => Task.FromResult(1));
            indexManager.Setup(i => i.FindArchiveRecord("999", false)).Returns(() => new ElasticArchiveRecord());

            // Act
            response = requestClient.Request(new PrepareAssetRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "5",
                AssetId = "usuallySomeGuid"
            });

            // Wait for the results
            var message = await response;
            await findArchiveRecordTask;
            await prepareAssetTask;


            // Assert
            message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
            assetManager.Verify(a => a.RegisterJobInPreparationQueue("999", "usuallySomeGuid", AufbereitungsArtEnum.Download,
                AufbereitungsServices.AssetService,
                It.IsAny<List<ElasticArchiveRecordPackage>>(), It.IsAny<object>()), () => Times.Once());
        }
    }
}
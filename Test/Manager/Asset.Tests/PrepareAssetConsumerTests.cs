using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Index;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PrepareAssetConsumerTests 
    {
        private Mock<IAssetManager> assetManager;
        private Mock<IIndexManager> indexManager;
        private Mock<IRequestClient<FindArchiveRecordRequest>> indexClient;
        private ITestHarness harness;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            indexManager = new Mock<IIndexManager>();
            indexClient = new Mock<IRequestClient<FindArchiveRecordRequest>>();
            provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<PrepareAssetConsumer>();
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => indexManager.Object);
                    cfg.AddTransient(_ => indexClient.Object);
                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>();
        }

        [Test]
        public async Task DownloadPackage_message_is_not_sent_if_already_in_preparation()
        {
            // Arrange
            assetManager.Setup(e => e.CheckPreparationStatus("999"))
                .Returns(Task.FromResult(new PreparationStatus {PackageIsInPreparationQueue = true}));
            await harness.Start();

            var client = harness.GetRequestClient<PrepareAssetRequest>();

            // Act
            var result = await client.GetResponse<PrepareAssetResult>(new PrepareAssetRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "6"
            });
          
            // Assert
            result.Message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
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
            indexManager.Setup(i => i.FindArchiveRecord("999", false, false)).Returns(() => new ElasticArchiveRecord());

            var findArchiveRecordResponseMock = new Mock<Response<FindArchiveRecordResponse>>();

            findArchiveRecordResponseMock.SetupGet(x => x.Message).Returns(new FindArchiveRecordResponse
            {
                ArchiveRecordId = "999",
                ElasticArchiveRecord = new ElasticArchiveRecord()
            });
            indexClient.Setup(r =>
                r.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), new CancellationToken(), new RequestTimeout())).ReturnsAsync(findArchiveRecordResponseMock.Object);


            await harness.Start();

            var client = harness.GetRequestClient<PrepareAssetRequest>();

            // Act
            var result = await client.GetResponse<PrepareAssetResult>(new PrepareAssetRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "5",
                AssetId = "usuallySomeGuid"
            });
            
            // Assert
            result.Message.Status.Should().Be(AssetDownloadStatus.InPreparationQueue);
            assetManager.Verify(a => a.RegisterJobInPreparationQueue("999", "usuallySomeGuid", AufbereitungsArtEnum.Download,
                AufbereitungsServices.AssetService,
                It.IsAny<List<ElasticArchiveRecordPackage>>(), It.IsAny<object>()), Times.Once);
        }
    }
}
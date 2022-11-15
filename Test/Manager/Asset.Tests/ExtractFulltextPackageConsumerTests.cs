using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Index;
using CMI.Manager.Index.Consumer;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class ExtractFulltextPackageConsumerTests
    {
        private ITestHarness harness;
        private Mock<IAssetManager> assetManager;
        private Mock<IIndexManager> indexManager;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            indexManager = new Mock<IIndexManager>();
            provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<ExtractFulltextPackageConsumer>();
                    cfg.AddConsumer<UpdateArchiveRecordConsumer>().Endpoint(e => e.Name = BusConstants.IndexManagerUpdateArchiveRecordMessageQueue);
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => indexManager.Object);
                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [Test]
        public async Task If_fulltext_extraction_is_fail_then_sync_is_failed()
        {
            // Arrange
            await harness.Start();
            var ar = new ArchiveRecord { ArchiveRecordId = "2" };
            assetManager.Setup(e => e.ExtractFulltext(1234, It.IsAny<ArchiveRecord>(), It.IsAny<int>())).ReturnsAsync(false);

            // Act
            await harness.Bus.Publish(new ArchiveRecordExtractFulltextFromPackage
            {
                MutationId = 1234,
                ArchiveRecord = ar
            });

            // Assert
            Assert.IsTrue(await harness.Published.Any<IArchiveRecordUpdated>());
            Assert.IsTrue(await harness.Consumed.Any<IArchiveRecordExtractFulltextFromPackage>());
            var consumerHarness = harness.GetConsumerHarness<ExtractFulltextPackageConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IArchiveRecordExtractFulltextFromPackage>());
            await harness.Stop();
        }


        [Test]
        public async Task If_fulltext_extraction_is_success_update_index_message_is_sent()
        {
            // Arrange
            await harness.Start();
            var ar = new ArchiveRecord { ArchiveRecordId = "1" };
            assetManager.Setup(e => e.ExtractFulltext(123, It.IsAny<ArchiveRecord>(), It.IsAny<int>())).ReturnsAsync(true);

            // Act
            await harness.Bus.Publish(new ArchiveRecordExtractFulltextFromPackage
            {
                MutationId = 123,
                ArchiveRecord = ar
            });

            // Assert
            Assert.IsTrue(await harness.Consumed.Any<IArchiveRecordExtractFulltextFromPackage>());
            var consumerHarness = harness.GetConsumerHarness<ExtractFulltextPackageConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IArchiveRecordExtractFulltextFromPackage>());
            var updateConsumer = harness.GetConsumerHarness<UpdateArchiveRecordConsumer>();
            Assert.That(await updateConsumer.Consumed.Any<IUpdateArchiveRecord>());
            await harness.Stop();
        }
    }
}
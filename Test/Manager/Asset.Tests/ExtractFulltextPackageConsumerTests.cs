using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class ExtractFulltextPackageConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IArchiveRecordUpdated>> archiveRecordUpdatedConsumer = new Mock<IConsumer<IArchiveRecordUpdated>>();

        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<IConsumer<IUpdateArchiveRecord>> updateArchiveRecordConsumer = new Mock<IConsumer<IUpdateArchiveRecord>>();
        private Task<ConsumeContext<IArchiveRecordUpdated>> archiveRecordUpdatedHandled;
        private Task<ConsumeContext<IArchiveRecordExtractFulltextFromPackage>> extractFulltextTask;
        private Task<ConsumeContext<IUpdateArchiveRecord>> updateArchiveRecordHandled;

        public ExtractFulltextPackageConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            assetManager.Reset();
            updateArchiveRecordConsumer.Reset();
            archiveRecordUpdatedConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            extractFulltextTask = Handler<IArchiveRecordExtractFulltextFromPackage>(configurator,
                context => new ExtractFulltextPackageConsumer(assetManager.Object, Bus).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.IndexManagerUpdateArchiveRecordMessageQueue, ec =>
            {
                ec.Consumer(() => updateArchiveRecordConsumer.Object);
                updateArchiveRecordHandled = Handled<IUpdateArchiveRecord>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
            {
                ec.Consumer(() => archiveRecordUpdatedConsumer.Object);
                archiveRecordUpdatedHandled = Handled<IArchiveRecordUpdated>(ec);
            });
        }


        [Test]
        public async Task If_fulltext_extraction_is_fail_then_sync_is_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "2"};
            assetManager.Setup(e => e.ExtractFulltext(1234, It.IsAny<ArchiveRecord>(), It.IsAny<int>())).ReturnsAsync(false);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordExtractFulltextFromPackage>(new
            {
                MutationId = 1234,
                ArchiveRecord = ar
            });

            // Wait for the results
            await extractFulltextTask;
            var context = await archiveRecordUpdatedHandled;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(1234);
        }

        [Test]
        public async Task If_fulltext_extraction_is_success_update_index_message_is_sent()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "1"};
            assetManager.Setup(e => e.ExtractFulltext(123, It.IsAny<ArchiveRecord>(), It.IsAny<int>())).ReturnsAsync(true);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordExtractFulltextFromPackage>(new
            {
                MutationId = 123,
                ArchiveRecord = ar
            });

            // Wait for the results
            await extractFulltextTask;
            var context = await updateArchiveRecordHandled;

            // Assert
            context.Message.MutationId.Should().Be(123);
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be("1");
        }
    }
}
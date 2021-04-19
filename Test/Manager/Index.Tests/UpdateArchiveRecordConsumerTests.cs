using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Index.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    public class UpdateArchiveRecordConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<IUpdateArchiveRecord>> updateArchiveRecordConsumer = new Mock<IConsumer<IUpdateArchiveRecord>>();
        private Task<ConsumeContext<IArchiveRecordUpdated>> archiveRecordUpdatedTask;
        private Task<ConsumeContext<IUpdateArchiveRecord>> updateArchiveReocrdTask;

        public UpdateArchiveRecordConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            indexManager.Reset();
            updateArchiveRecordConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            updateArchiveReocrdTask = Handler<IUpdateArchiveRecord>(configurator,
                context => new UpdateArchiveRecordConsumer(indexManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
            {
                ec.Consumer(() => updateArchiveRecordConsumer.Object);
                archiveRecordUpdatedTask = Handled<IArchiveRecordUpdated>(ec);
            });
        }

        [Test]
        public async Task If_Update_throws_error_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "345"};
            var mutationId = 666;
            var errMsg = "Hi I'm an error";
            indexManager.Setup(e => e.UpdateArchiveRecord(It.IsAny<ConsumeContext<IUpdateArchiveRecord>>())).Throws(new Exception(errMsg));

            // Act
            await InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await updateArchiveReocrdTask;
            var context = await archiveRecordUpdatedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(errMsg);
        }

        [Test]
        public async Task If_Update_succeeds_Sync_process_is_set_to_success()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "3245", Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> {"BAR"},
                    PrimaryDataFulltextAccessToken = new List<string> {"BAR"},
                    PrimaryDataDownloadAccessToken = new List<string> {"BAR"}
                }
            };
            var mutationId = 124;
            indexManager.Setup(e => e.UpdateArchiveRecord(It.IsAny<ConsumeContext<IUpdateArchiveRecord>>()));

            // Act
            await InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await updateArchiveReocrdTask;
            var context = await archiveRecordUpdatedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(true);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(null);
        }
    }
}
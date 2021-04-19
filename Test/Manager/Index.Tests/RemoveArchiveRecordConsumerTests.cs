using System;
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
    public class RemoveArchiveRecordConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<IRemoveArchiveRecord>> removeArchiveRecordConsumer = new Mock<IConsumer<IRemoveArchiveRecord>>();
        private Task<ConsumeContext<IArchiveRecordRemoved>> archiveRecordRemovedTask;
        private Task<ConsumeContext<IRemoveArchiveRecord>> removeArchiveReocrdTask;

        public RemoveArchiveRecordConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            indexManager.Reset();
            removeArchiveRecordConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            removeArchiveReocrdTask = Handler<IRemoveArchiveRecord>(configurator,
                context => new RemoveArchiveRecordConsumer(indexManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
            {
                ec.Consumer(() => removeArchiveRecordConsumer.Object);
                archiveRecordRemovedTask = Handled<IArchiveRecordRemoved>(ec);
            });
        }

        [Test]
        public async Task If_Remove_throws_error_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "3457"};
            var mutationId = 6667;
            var errMsg = "Hi I'm an error";
            indexManager.Setup(e => e.RemoveArchiveRecord(It.IsAny<ConsumeContext<IRemoveArchiveRecord>>())).Throws(new Exception(errMsg));

            // Act
            await InputQueueSendEndpoint.Send<IRemoveArchiveRecord>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await removeArchiveReocrdTask;
            var context = await archiveRecordRemovedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(errMsg);
        }

        [Test]
        public async Task If_Remove_succeeds_Sync_process_is_set_to_success()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "32245"};
            var mutationId = 1243;
            indexManager.Setup(e => e.RemoveArchiveRecord(It.IsAny<ConsumeContext<IRemoveArchiveRecord>>()));

            // Act
            await InputQueueSendEndpoint.Send<IRemoveArchiveRecord>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await removeArchiveReocrdTask;
            var context = await archiveRecordRemovedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(true);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(null);
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Index.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    public class RemoveArchiveRecordConsumerTests
    {
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<IRemoveArchiveRecord>> removeArchiveRecordConsumer = new Mock<IConsumer<IRemoveArchiveRecord>>();

        [SetUp]
        public void Setup()
        {
            indexManager.Reset();
            removeArchiveRecordConsumer.Reset();
        }

        [Test]
        public async Task If_Remove_throws_error_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "3457"};
            var mutationId = 6667;
            var errMsg = "Hi I'm an error";
            indexManager.Setup(e => e.RemoveArchiveRecord(It.IsAny<ConsumeContext<IRemoveArchiveRecord>>())).Throws(new Exception(errMsg));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new RemoveArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IRemoveArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IRemoveArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IRemoveArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordRemoved>());
                var message = harness.Published.Select<IArchiveRecordRemoved>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                // Assert
                message.Context.Message.ActionSuccessful.Should().Be(false);
                message.Context.Message.MutationId.Should().Be(mutationId);
                message.Context.Message.ErrorMessage.Should().Be(errMsg);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_Remove_succeeds_Sync_process_is_set_to_success()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "32245"};
            var mutationId = 1243;
            indexManager.Setup(e => e.RemoveArchiveRecord(It.IsAny<ConsumeContext<IRemoveArchiveRecord>>()));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new RemoveArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IRemoveArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IRemoveArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IRemoveArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordRemoved>());
                var message = harness.Published.Select<IArchiveRecordRemoved>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ActionSuccessful.Should().Be(true);
                message.Context.Message.MutationId.Should().Be(mutationId);
                message.Context.Message.ErrorMessage.Should().Be(null);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class ArchiveRecordRemovedConsumerTests
    {
        private readonly Mock<IConsumer<IArchiveRecordRemoved>> archiveRecordRemovedConsumer = new Mock<IConsumer<IArchiveRecordRemoved>>();
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            archiveRecordRemovedConsumer.Reset();
        }

        [Test]
        public async Task If_sync_success_update_mutation_table_with_success()
        {
            // Arrange
            long mutationId = 666;
            var status = new MutationStatusInfo
            {
                MutationId = mutationId,
                NewStatus = ActionStatus.SyncCompleted
            };
            harvestManager.Setup(e => e.UpdateMutationStatus(status));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new ArchiveRecordRemovedConsumer(harvestManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordRemoved>(new
                {
                    MutationId = mutationId,
                    ActionSuccessful = true
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IArchiveRecordRemoved>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IArchiveRecordRemoved>());

                // Assert
                harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                    f.MutationId == mutationId &&
                    f.NewStatus == ActionStatus.SyncCompleted
                )), Times.Once);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_sync_failed_update_mutation_table_with_failure()
        {
            // Arrange
            long mutationId = 666;
            var status = new MutationStatusInfo
            {
                MutationId = mutationId,
                NewStatus = ActionStatus.SyncFailed
            };
            harvestManager.Setup(e => e.UpdateMutationStatus(status));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new ArchiveRecordRemovedConsumer(harvestManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordRemoved>(new
                {
                    MutationId = mutationId,
                    ActionSuccessful = false,
                    ErrorMessage = "My little error"
                });



                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IArchiveRecordRemoved>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IArchiveRecordRemoved>());

                // Assert
                harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                    f.MutationId == mutationId &&
                    f.NewStatus == ActionStatus.SyncFailed &&
                    f.ErrorMessage == "My little error"
                )), Times.Once);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}
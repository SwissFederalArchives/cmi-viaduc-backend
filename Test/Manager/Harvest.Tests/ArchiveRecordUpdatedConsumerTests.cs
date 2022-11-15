using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class ArchiveRecordUpdatedConsumerTests
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
        }
        
        [Test]
        public async Task If_sync_success_update_mutation_table_with_success()
        {
            var harness = new InMemoryTestHarness();
            try
            {
                // Arrange
                long mutationId = 666;
                var status = new MutationStatusInfo
                {
                    MutationId = mutationId,
                    NewStatus = ActionStatus.SyncCompleted,
                    ChangeFromStatus = ActionStatus.SyncInProgress
                };
                harvestManager.Setup(e => e.UpdateMutationStatus(status));
                var archiveRecordUpdatedConsumer = harness.Consumer(() => new ArchiveRecordUpdatedConsumer(harvestManager.Object));

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordUpdated>(new
                {
                    MutationId = mutationId,
                    ActionSuccessful = true
                });

                // Wait for the results
                Assert.True(await archiveRecordUpdatedConsumer.Consumed.Any<IArchiveRecordUpdated>());

                // Assert
                harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                    f.MutationId == mutationId &&
                    f.NewStatus == ActionStatus.SyncCompleted &&
                    f.ChangeFromStatus == ActionStatus.SyncInProgress
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
            var harness = new InMemoryTestHarness();

            try
            {
                long mutationId = 666;
                var status = new MutationStatusInfo
                {
                    MutationId = mutationId,
                    NewStatus = ActionStatus.SyncFailed,
                    ChangeFromStatus = ActionStatus.SyncInProgress
                };
                harvestManager.Setup(e => e.UpdateMutationStatus(status));
                var archiveRecordUpdatedConsumer = harness.Consumer(() => new ArchiveRecordUpdatedConsumer(harvestManager.Object));

                await harness.Start();
                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordUpdated>(new
                {
                    MutationId = mutationId,
                    ActionSuccessful = false,
                    ErrorMessage = "My little error"
                });

                // Wait for the results
                Assert.True(await archiveRecordUpdatedConsumer.Consumed.Any<IArchiveRecordUpdated>());

                // Assert
                harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                    f.MutationId == mutationId &&
                    f.NewStatus == ActionStatus.SyncFailed &&
                    f.ChangeFromStatus == ActionStatus.SyncInProgress &&
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
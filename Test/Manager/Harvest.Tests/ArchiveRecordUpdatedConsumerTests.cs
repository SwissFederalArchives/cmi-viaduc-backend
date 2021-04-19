using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class ArchiveRecordUpdatedConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IArchiveRecordUpdated>> archiveRecordUpdatedConsumer = new Mock<IConsumer<IArchiveRecordUpdated>>();

        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private Task<ConsumeContext<IArchiveRecordUpdated>> archiveRecordUpdatedTask;

        public ArchiveRecordUpdatedConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            archiveRecordUpdatedConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            archiveRecordUpdatedTask = Handler<IArchiveRecordUpdated>(configurator,
                context => new ArchiveRecordUpdatedConsumer(harvestManager.Object).Consume(context));
        }

        [Test]
        public async Task If_sync_success_update_mutation_table_with_success()
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

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordUpdated>(new
            {
                MutationId = mutationId,
                ActionSuccessful = true
            });

            // Wait for the results
            await archiveRecordUpdatedTask;

            // Assert
            harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                f.MutationId == mutationId &&
                f.NewStatus == ActionStatus.SyncCompleted &&
                f.ChangeFromStatus == ActionStatus.SyncInProgress
            )), Times.Once);
        }

        [Test]
        public async Task If_sync_failed_update_mutation_table_with_failure()
        {
            // Arrange
            long mutationId = 666;
            var status = new MutationStatusInfo
            {
                MutationId = mutationId,
                NewStatus = ActionStatus.SyncFailed,
                ChangeFromStatus = ActionStatus.SyncInProgress
            };
            harvestManager.Setup(e => e.UpdateMutationStatus(status));

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordUpdated>(new
            {
                MutationId = mutationId,
                ActionSuccessful = false,
                ErrorMessage = "My little error"
            });

            // Wait for the results
            await archiveRecordUpdatedTask;

            // Assert
            harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                f.MutationId == mutationId &&
                f.NewStatus == ActionStatus.SyncFailed &&
                f.ChangeFromStatus == ActionStatus.SyncInProgress &&
                f.ErrorMessage == "My little error"
            )), Times.Once);
        }
    }
}
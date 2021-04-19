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
    public class ArchiveRecordRemovedConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IArchiveRecordRemoved>> archiveRecordRemovedConsumer = new Mock<IConsumer<IArchiveRecordRemoved>>();

        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private Task<ConsumeContext<IArchiveRecordRemoved>> archiveRecordRemovedTask;

        public ArchiveRecordRemovedConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            archiveRecordRemovedConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            archiveRecordRemovedTask = Handler<IArchiveRecordRemoved>(configurator,
                context => new ArchiveRecordRemovedConsumer(harvestManager.Object).Consume(context));
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

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordRemoved>(new
            {
                MutationId = mutationId,
                ActionSuccessful = true
            });

            // Wait for the results
            await archiveRecordRemovedTask;

            // Assert
            harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                f.MutationId == mutationId &&
                f.NewStatus == ActionStatus.SyncCompleted
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
                NewStatus = ActionStatus.SyncFailed
            };
            harvestManager.Setup(e => e.UpdateMutationStatus(status));

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordRemoved>(new
            {
                MutationId = mutationId,
                ActionSuccessful = false,
                ErrorMessage = "My little error"
            });

            // Wait for the results
            await archiveRecordRemovedTask;

            // Assert
            harvestManager.Verify(e => e.UpdateMutationStatus(It.Is<MutationStatusInfo>(f =>
                f.MutationId == mutationId &&
                f.NewStatus == ActionStatus.SyncFailed &&
                f.ErrorMessage == "My little error"
            )), Times.Once);
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class ArchiveDatabaseResyncConsumerTests 
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private InMemoryTestHarness harness;
        
        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
            harvestManager.Reset();
        }
        

        [Test]
        public async Task If_resync_is_requested_the_init_method_is_called()
        {
            // Arrange
            var info = new ResyncRequestInfo {Username = "the username", IssueDate = DateTime.Today};
            var resyncArchiveDatabase = new Mock<IResyncArchiveDatabase>();
            resyncArchiveDatabase.SetupGet(r => r.RequestInfo).Returns(info);
            harvestManager.Setup(e => e.InitiateFullResync(It.IsAny<ResyncRequestInfo>())).Returns(999);
            var consumer = new ArchiveDatabaseResyncConsumer(harvestManager.Object);
            var context = new Mock<ConsumeContext<IResyncArchiveDatabase>>();
            context.SetupGet(x => x.Message).Returns(resyncArchiveDatabase.Object);

            // Act
            await consumer.Consume(context.Object);

            // Assert
            harvestManager.Verify(e => e.InitiateFullResync(It.IsAny<ResyncRequestInfo>()));
            context.Verify(c => c.Publish<IResyncArchiveDatabaseStarted>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
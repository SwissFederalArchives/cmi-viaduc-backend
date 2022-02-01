using System;
using System.Threading.Tasks;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class ArchiveDatabaseResyncConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();

        private readonly Mock<IConsumer<IResyncArchiveDatabaseStarted>> resyncArchiveDatabaseConsumer =
            new Mock<IConsumer<IResyncArchiveDatabaseStarted>>();

        private Task<ConsumeContext<IResyncArchiveDatabaseStarted>> resyncArchiveDatabaseStartedTask;
        private Task<ConsumeContext<IResyncArchiveDatabase>> resyncArchiveDatabaseTask;

        public ArchiveDatabaseResyncConsumerTests()
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            resyncArchiveDatabaseTask = Handler<IResyncArchiveDatabase>(configurator,
                context => new ArchiveDatabaseResyncConsumer(harvestManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint($"{nameof(ArchiveDatabaseResyncConsumerTests)}", ec =>
            {
                ec.Consumer(() => resyncArchiveDatabaseConsumer.Object);
                resyncArchiveDatabaseStartedTask = Handled<IResyncArchiveDatabaseStarted>(ec);
            });
        }

        [Test]
        public async Task If_resync_is_requested_the_init_method_is_called()
        {
            // Arrange
            var info = new ResyncRequestInfo {Username = "the username", IssueDate = DateTime.Today};
            harvestManager.Setup(e => e.InitiateFullResync(It.IsAny<ResyncRequestInfo>())).Returns(999);

            // Act
            await InputQueueSendEndpoint.Send<IResyncArchiveDatabase>(new
            {
                RequestInfo = info
            });

            // Wait for the results
            await resyncArchiveDatabaseTask;
            var context = await resyncArchiveDatabaseStartedTask;

            // Assert
            context.Message.InsertedRecords.Should().Be(999);
            harvestManager.Verify(e => e.InitiateFullResync(It.IsAny<ResyncRequestInfo>()));
        }
    }
}
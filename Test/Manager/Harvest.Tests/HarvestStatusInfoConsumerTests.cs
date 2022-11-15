using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class HarvestStatusInfoConsumerTests
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private InMemoryTestHarness harness;
        private GetHarvestStatusInfoResult result;


        public HarvestStatusInfoConsumerTests()
        {
            harness = new InMemoryTestHarness();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
        }
        
        [Test]
        public async Task Request_responds_with_answer()
        {
            // Arrange
            harvestManager.Setup(e => e.GetStatusInfo(It.IsAny<QueryDateRangeEnum>())).Returns(new HarvestStatusInfo
            {
                NumberOfRecordsCurrentlySyncing = 1,
                NumberOfRecordsWithSyncFailure = 2,
                NumberOfRecordsWaitingForSync = 3,
                NumberOfRecordsWithSyncSuccess = 4,
                TotalNumberOfRecordsWithSyncFailure = 10,
                TotalNumberOfRecordsCurrentlySyncing = 11,
                TotalNumberOfRecordsWithSyncSuccess = 12,
                TotalNumberOfRecordsWaitingForSync = 13
            });
            var consumer = new HarvestStatusInfoConsumer(harvestManager.Object);
            var context = new Mock<ConsumeContext<GetHarvestStatusInfo>>(); 
            context.Setup(c => c.RespondAsync<GetHarvestStatusInfoResult>(It.IsAny<GetHarvestStatusInfoResult>())).Returns(GetTask);

            context.SetupGet(x => x.Message).Returns(new GetHarvestStatusInfo
            {
                DateRange = QueryDateRangeEnum.LastHour
            });
           
            // Act
            await consumer.Consume(context.Object);

            // Assert
            result.Result.NumberOfRecordsCurrentlySyncing.Should().Be(1);
            result.Result.NumberOfRecordsWithSyncFailure.Should().Be(2);
            result.Result.NumberOfRecordsWaitingForSync.Should().Be(3);
            result.Result.NumberOfRecordsWithSyncSuccess.Should().Be(4);
            harvestManager.Verify(e => e.GetStatusInfo(It.IsAny<QueryDateRangeEnum>()));
        }

        private Task GetTask(GetHarvestStatusInfoResult result)
        {
            this.result = result;
            return Task.CompletedTask;
        }
    }
}
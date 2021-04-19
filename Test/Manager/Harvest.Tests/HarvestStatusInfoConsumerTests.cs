using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class HarvestStatusInfoConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private IRequestClient<GetHarvestStatusInfo, GetHarvestStatusInfoResult> client;

        public HarvestStatusInfoConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            client = CreateRequestClient<GetHarvestStatusInfo, GetHarvestStatusInfoResult>();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            Handler<GetHarvestStatusInfo>(configurator, context => new HarvestStatusInfoConsumer(harvestManager.Object).Consume(context));
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

            // Act
            var response = await client.Request(new GetHarvestStatusInfo {DateRange = QueryDateRangeEnum.LastHour});

            // Assert
            response.Result.NumberOfRecordsCurrentlySyncing.Should().Be(1);
            response.Result.NumberOfRecordsWithSyncFailure.Should().Be(2);
            response.Result.NumberOfRecordsWaitingForSync.Should().Be(3);
            response.Result.NumberOfRecordsWithSyncSuccess.Should().Be(4);
            harvestManager.Verify(e => e.GetStatusInfo(It.IsAny<QueryDateRangeEnum>()));
        }
    }
}
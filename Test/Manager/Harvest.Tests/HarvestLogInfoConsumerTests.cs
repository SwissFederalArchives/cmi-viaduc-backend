using System;
using System.Collections.Generic;
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
    public class HarvestLogInfoConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private IRequestClient<GetHarvestLogInfo, GetHarvestLogInfoResult> client;

        public HarvestLogInfoConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            client = CreateRequestClient<GetHarvestLogInfo, GetHarvestLogInfoResult>();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            Handler<GetHarvestLogInfo>(configurator, context => new HarvestLogInfoConsumer(harvestManager.Object).Consume(context));
        }


        [Test]
        public async Task Request_responds_with_answer()
        {
            // Arrange
            harvestManager.Setup(e => e.GetLogInfo(It.IsAny<HarvestLogInfoRequest>())).Returns(new HarvestLogInfoResult
                {ResultSet = new List<HarvestLogInfo>(), TotalResultSetSize = 1000});

            // Act
            var response = await client.Request(new GetHarvestLogInfo {Request = new HarvestLogInfoRequest {ArchiveRecordIdFilter = "123"}});

            // Assert
            response.Result.TotalResultSetSize.Should().Be(1000);
            harvestManager.Verify(e => e.GetLogInfo(It.IsAny<HarvestLogInfoRequest>()));
        }
    }
}
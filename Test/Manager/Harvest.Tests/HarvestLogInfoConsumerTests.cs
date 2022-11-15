using System;
using System.Collections.Generic;
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
    public class HarvestLogInfoConsumerTests
    {
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private InMemoryTestHarness harness;
        private GetHarvestLogInfoResult getHarvestLogInfoResult;

       [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
            harvestManager.Reset();
        }

        [Test]
        public async Task Request_responds_with_answer()
        {
            // Arrange
            harvestManager.Setup(e => e.GetLogInfo(It.IsAny<HarvestLogInfoRequest>())).Returns(new HarvestLogInfoResult
                {ResultSet = new List<HarvestLogInfo>(), TotalResultSetSize = 1000});
            var consumer = new HarvestLogInfoConsumer(harvestManager.Object);
            var request = new Mock<GetHarvestLogInfo>();
            var context = new Mock<ConsumeContext<GetHarvestLogInfo>>();
            context.SetupGet(x => x.Message).Returns(request.Object);
            context.Setup(c => c.RespondAsync<CMI.Contract.Messaging.GetHarvestLogInfoResult>(It.IsAny<GetHarvestLogInfoResult>())).Returns(GetTask);

            // Act
            await consumer.Consume(context.Object);
            // Assert
            getHarvestLogInfoResult.Result.TotalResultSetSize.Should().Be(1000);
            harvestManager.Verify(e => e.GetLogInfo(It.IsAny<HarvestLogInfoRequest>()));
        }

        private Task GetTask(GetHarvestLogInfoResult result)
        {
            getHarvestLogInfoResult = result ;
            return Task.CompletedTask;
        }
        
    }
}
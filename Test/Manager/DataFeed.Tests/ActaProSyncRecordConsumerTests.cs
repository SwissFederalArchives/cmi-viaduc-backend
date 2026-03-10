using CMI.Contract.Messaging;
using CMI.Manager.DataFeed.Consumers;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CMI.Manager.DataFeed.Tests
{
    public class ActaProSyncRecordConsumerTests
    {
        private Mock<IDataFeedManager> dataFeedManagerMock;
        private InMemoryTestHarness harness;

        [SetUp]
        public void Setup()
        {
            dataFeedManagerMock = new Mock<IDataFeedManager>();
            harness = new InMemoryTestHarness();
        }

        [TearDown]
        public async Task Teardown()
        {
            if (harness != null)
                await harness.Stop();
        }

        [Test]
        public async Task Consumer_should_handle_ActaProSyncRecord_message()
        {
            // Arrange
            var archiveRecordId = "abc-123";
            var action = "Update";

            var consumer = harness.Consumer(() =>
                new ActaProSyncRecordConsumer(dataFeedManagerMock.Object));

            await harness.Start();

            // Act
            await harness.InputQueueSendEndpoint.Send(new ActaProSyncRecord
            {
                ArchiveRecordId = archiveRecordId,
                Action = action
            });

            // Assert: Did the bus consume the message?
            Assert.That(await harness.Consumed.Any<ActaProSyncRecord>(), Is.True);

            // Assert: Did the actual consumer process it?
            Assert.That(await consumer.Consumed.Any<ActaProSyncRecord>(), Is.True);

            // Assert: Was the manager called with correct data?
            dataFeedManagerMock.Verify(m =>
                m.HandleActaProSyncRecordAsync(It.Is<ActaProSyncRecord>(msg =>
                    msg.ArchiveRecordId == archiveRecordId &&
                    msg.Action == action)), Times.Once);
        }
    }
}

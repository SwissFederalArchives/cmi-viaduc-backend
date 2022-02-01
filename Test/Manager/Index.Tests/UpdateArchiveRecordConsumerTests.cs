using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Index.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    public class UpdateArchiveRecordConsumerTests
    {
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<IUpdateArchiveRecord>> updateArchiveRecordConsumer = new Mock<IConsumer<IUpdateArchiveRecord>>();

        [SetUp]
        public void Setup()
        {
            indexManager.Reset();
            updateArchiveRecordConsumer.Reset();
        }

 

        [Test]
        public async Task If_Update_throws_error_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "345"};
            var mutationId = 666;
            var errMsg = "Hi I'm an error";
            indexManager.Setup(e => e.UpdateArchiveRecord(It.IsAny<ConsumeContext<IUpdateArchiveRecord>>())).Throws(new Exception(errMsg));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new UpdateArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var message = harness.Published.Select<IArchiveRecordUpdated>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ActionSuccessful.Should().Be(false);
                message.Context.Message.MutationId.Should().Be(mutationId);
                message.Context.Message.ErrorMessage.Should().Be(errMsg);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_Update_succeeds_Sync_process_is_set_to_success()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "3245", Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> {"BAR"},
                    PrimaryDataFulltextAccessToken = new List<string> {"BAR"},
                    PrimaryDataDownloadAccessToken = new List<string> {"BAR"}
                }
            };
            var mutationId = 124;
            indexManager.Setup(e => e.UpdateArchiveRecord(It.IsAny<ConsumeContext<IUpdateArchiveRecord>>()));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new UpdateArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var message = harness.Published.Select<IArchiveRecordUpdated>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ActionSuccessful.Should().Be(true);
                message.Context.Message.MutationId.Should().Be(mutationId);
                message.Context.Message.ErrorMessage.Should().Be(null);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}
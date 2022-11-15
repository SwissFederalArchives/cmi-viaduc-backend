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
    [TestFixture]
    public class AnonymizationArchiveRecordConsumerTests
    {
        private readonly Mock<IIndexManager> indexManager = new Mock<IIndexManager>();
        private readonly Mock<IConsumer<AnonymizationArchiveRecordConsumer>> updateArchiveRecordConsumer = new Mock<IConsumer<AnonymizationArchiveRecordConsumer>>();

        [SetUp]
        public void Setup()
        {
            indexManager.Reset();
            updateArchiveRecordConsumer.Reset();
        }

        [Test]
        public async Task If_Anonymization_succeeds_done()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "3245", Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> { "BAR" },
                    PrimaryDataFulltextAccessToken = new List<string> { "BAR" },
                    PrimaryDataDownloadAccessToken = new List<string> { "BAR" },
                    FieldAccessToken = new List<string> { "BAR" }
                }
            };
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "3245",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldTokens = new List<string> { "BAR" },
                IsAnonymized = true
            };
            var mutationId = 124;
            indexManager.Setup(e => e.AnonymizeArchiveRecordAsync(It.IsAny<ElasticArchiveDbRecord>())).ReturnsAsync(elasticArchiveRecord);
            
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new AnonymizationArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IAnonymizationArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,
                    ElasticArchiveDbRecord = elasticArchiveRecord
                });

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IAnonymizationArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IAnonymizationArchiveRecord>());
                Assert.That(harness.Sent.Count() == 2);
                Assert.That(harness.Sent.Select<IUpdateArchiveRecord>().Any());
                Assert.That(harness.Sent.Select<IAnonymizationArchiveRecord>().Any());
            }
            finally
            {
                await harness.Stop();
            }
        }
        [Test]
        public async Task If_Anonymization_Throws_Exception()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> { "BAR" },
                    PrimaryDataFulltextAccessToken = new List<string> { "BAR" },
                    PrimaryDataDownloadAccessToken = new List<string> { "BAR" },
                    FieldAccessToken = new List<string> { "BAR" }
                }
            };
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "3245",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldTokens = new List<string> { "BAR" },
                IsAnonymized = true
            };
            var mutationId = 124;
            indexManager.Setup(e =>  e.AnonymizeArchiveRecordAsync(It.IsAny<ElasticArchiveDbRecord>())).Throws(new Exception());
             
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new AnonymizationArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IAnonymizationArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,
                    ElasticArchiveDbRecord = elasticArchiveRecord
                });

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IAnonymizationArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IAnonymizationArchiveRecord>());
                Assert.That(harness.Sent.Count() == 1);
                Assert.That(harness.Sent.Select<IAnonymizationArchiveRecord>().Any());
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var message = harness.Published.Select<IArchiveRecordUpdated>().FirstOrDefault();

                Assert.That(message != null);
                message.Context.Message.ActionSuccessful.Should().Be(false);
                message.Context.Message.MutationId.Should().Be(mutationId);
                message.Context.Message.ErrorMessage.Should().NotBeNull();
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

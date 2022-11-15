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
        public async Task If_Update_succeeds_Sync_process_is_set_to_success()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "3245", Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> { "BAR" },
                    PrimaryDataFulltextAccessToken = new List<string> { "BAR" },
                    PrimaryDataDownloadAccessToken = new List<string> { "BAR" },
                    FieldAccessToken = new List<string>( )
                }
            };
            var mutationId = 124;
            indexManager.Setup(e => e.ConvertArchiveRecord(It.IsAny<ArchiveRecord>())).Returns(new ElasticArchiveRecord());

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

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var message = harness.Published.Select<IArchiveRecordUpdated>().FirstOrDefault();

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
      
        [Test]
        public async Task If_Update_with_FieldAccessToken_call_AnonymizationArchiveRecord()
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
            var mutationId = 124;
            indexManager.Setup(e => e.ConvertArchiveRecord(ar)).Returns(new ElasticArchiveDbRecord());
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

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                Assert.That(harness.Sent.Count() == 2);
                Assert.That(harness.Sent.Select<IUpdateArchiveRecord>().Any());
                Assert.That(harness.Sent.Select<IAnonymizationArchiveRecord>().Any());

                var message = harness.Sent.Select<IAnonymizationArchiveRecord>().FirstOrDefault();
                Assert.That(message != null);
                message.Context.Message.MutationId.Should().Be(mutationId);

            }
            finally
            {
                await harness.Stop();
            }
        }
        
        [Test]
        public async Task If_update_message_contains_elasticDbRecord_update_record_with_data_from_elasticArchiveDbRecord()
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
            var elasticArchiveDbRecord = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "3245",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldAccessTokens = new List<string> { "BAR" },
                IsAnonymized = true,
                UnanonymizedFields = new UnanonymizedFields()
            };
            var elasticArchiveNonDbRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = "3245",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldAccessTokens = new List<string> { "BAR" },
                IsAnonymized = false,
                ParentContentInfos = new List<ElasticParentContentInfo>(),
                ArchiveplanContext = new List<ElasticArchiveplanContextItem>()
            };

            var mutationId = 254;
            indexManager.Setup(e => e.ConvertArchiveRecord(It.IsAny<ArchiveRecord>())).Returns(elasticArchiveNonDbRecord);
            indexManager.Setup(e => e.UpdateArchiveRecord(elasticArchiveDbRecord));
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new UpdateArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,
                    PrimaerdatenAuftragId = 1235,
                    ElasticArchiveDbRecord = elasticArchiveDbRecord
                });

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                Assert.That(harness.Sent.Count() == 3);
                Assert.That(harness.Sent.Select<IUpdateArchiveRecord>().Any());
                Assert.That(harness.Sent.Select<RecalcIndivTokens>().Any());
                Assert.That(harness.Sent.Select<IUpdatePrimaerdatenAuftragStatus>().Any());
                indexManager.Verify(i => i.UpdateArchiveRecord(It.IsAny<ElasticArchiveDbRecord>()), Times.Once());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var message = harness.Published.Select<IArchiveRecordUpdated>().FirstOrDefault();

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

        [Test]
        public async Task If_Update_with_missing_field_access_tokens_inArchiveRecord_causes_exception_and_unsuccessful_update()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "4533",
                Display = new ArchiveRecordDisplay(),
                ElasticPrimaryData = new List<ElasticArchiveRecordPackage>()
            };

            var mutationId = 124;
            indexManager.Setup(e => e.ConvertArchiveRecord(ar)).Returns(new ElasticArchiveDbRecord());

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new UpdateArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,

                    PrimaerdatenAuftragId = 2341
                });
                
                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IUpdateArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                Assert.That(harness.Sent.Count() == 2);
                Assert.That(harness.Sent.Select<IUpdateArchiveRecord>().Any());
                Assert.That(harness.Sent.Select<IUpdatePrimaerdatenAuftragStatus>().Any());

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

        [Test]
        public async Task If_unprotected_record_has_protected_parent_then_the_update_dependent_records_method_is_called()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "700",
                Display = new ArchiveRecordDisplay(),
                Security = new ArchiveRecordSecurity
                {
                    MetadataAccessToken = new List<string> { "BAR" },
                    PrimaryDataFulltextAccessToken = new List<string> { "BAR" },
                    PrimaryDataDownloadAccessToken = new List<string> { "BAR" },
                    FieldAccessToken = new List<string>(),
                }
            };

            var elasticRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = "700",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldAccessTokens = new List<string>(),
                IsAnonymized = false,
                ParentArchiveRecordId = "12",
                ArchiveplanContext = new List<ElasticArchiveplanContextItem>
                {
                    new() { ArchiveRecordId = "12", Title = "Protected Parent", Protected = true }
                }
            };

            indexManager.Setup(e => e.ConvertArchiveRecord(It.IsAny<ArchiveRecord>())).Returns(elasticRecord);

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new UpdateArchiveRecordConsumer(indexManager.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<IUpdateArchiveRecord>(new
                {
                    ArchiveRecord = ar,
                    MutationId = 1,
                });

                // Assert
                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<IUpdateArchiveRecord>());

                // Verify that update dependent records was called
                indexManager.Verify(i => i.DeletePossiblyExistingManuelleKorrektur(elasticRecord), Times.Once);
                indexManager.Verify(i => i.UpdateDependentRecords("12"), Times.Once);

            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

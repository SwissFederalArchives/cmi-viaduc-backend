using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using CMI.Manager.Harvest.Infrastructure;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class SyncArchiveRecordConsumerTests
    {
        private readonly Mock<ICachedHarvesterSetting> cachedHarvesterSetting = new Mock<ICachedHarvesterSetting>();
        private readonly Mock<IRequestClient<FindArchiveRecordRequest>> findArchiveRecordClient =
            new Mock<IRequestClient<FindArchiveRecordRequest>>();
        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private readonly Mock<IConsumer<ISyncArchiveRecord>> syncArchiveRecordConsumer = new Mock<IConsumer<ISyncArchiveRecord>>();

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            syncArchiveRecordConsumer.Reset();
            cachedHarvesterSetting.Reset();
        }

        [Test]
        public async Task If_delete_is_requested_record_is_removed_from_index()
        {
            // Arrange
            var archvieRecordId = "34599";
            var mutationId = 6616;
            var ar = new ArchiveRecord { ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = null } };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "delete"
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());
                var message = harness.Consumed.Select<ISyncArchiveRecord>().FirstOrDefault();

                // was the delete message sent
                Assert.That(await harness.Sent.Any<IRemoveArchiveRecord>());

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_with_no_primary_data_index_is_updated()
        {
            // Arrange
            var archvieRecordId = "345";
            var mutationId = 666;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = null },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord { ArchiveRecordId = archvieRecordId, PrimaryDataLink = null }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);

            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());
                var message = harness.Consumed.Select<ISyncArchiveRecord>().FirstOrDefault();

                // was the update message sent
                Assert.That(await harness.Sent.Any<IUpdateArchiveRecord>());

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_with_primary_data_and_elastic_record_with_no_primaryLink_package_metadata_is_appended()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = "Aip@DossierId" },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord { ArchiveRecordId = archvieRecordId, PrimaryDataLink = null }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);

            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());
                var message = harness.Consumed.Select<ISyncArchiveRecord>().FirstOrDefault();

                // was the append metadata message sent
                Assert.That(await harness.Sent.Any<IArchiveRecordAppendPackageMetadata>());

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_with_primary_data_and_elastic_record_with_different_primaryLink_package_metadata_is_appended()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = "Aip@DossierId" },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord { ArchiveRecordId = archvieRecordId, PrimaryDataLink = "DifferentAip@DossierId" }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);

            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());
                var message = harness.Consumed.Select<ISyncArchiveRecord>().FirstOrDefault();

                // was the append metadata message sent
                Assert.That(await harness.Sent.Any<IArchiveRecordAppendPackageMetadata>());

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task
            If_update_is_requested_with_primary_data_and_elastic_record_with_identical_primaryLink_record_is_indexed_with_existing_data()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = "Aip@DossierId" },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId, ElasticArchiveRecord = new ElasticArchiveRecord
                {
                    ArchiveRecordId = archvieRecordId, PrimaryDataLink = "Aip@DossierId", PrimaryData = new List<ElasticArchiveRecordPackage>
                            {
                                new ElasticArchiveRecordPackage {FileCount = 5, PackageId = "controlPackageId"}
                            }
                }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);

            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Sent.Any<IUpdateArchiveRecord>());
                var message = harness.Sent.Select<IUpdateArchiveRecord>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.ArchiveRecord.ElasticPrimaryData[0].FileCount.Should().Be(5);
                message.Context.Message.ArchiveRecord.ElasticPrimaryData[0].PackageId.Should().Be("controlPackageId");
                message.Context.Message.MutationId.Should().Be(mutationId);

            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task
            If_update_is_requested_with_primary_data_and_elastic_record_with_identical_primaryLink_record_but_reindex_is_enforced_package_metadata_is_appended()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = "Aip@DossierId" },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            cachedHarvesterSetting.Setup(s => s.EnableFullResync()).Returns(true);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId, ElasticArchiveRecord = new ElasticArchiveRecord
                {
                    ArchiveRecordId = archvieRecordId, PrimaryDataLink = "Aip@DossierId", PrimaryData = new List<ElasticArchiveRecordPackage>
                            {
                                new ElasticArchiveRecordPackage {FileCount = 5, PackageId = "controlPackageId"}
                            }
                }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);
            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Sent.Any<IArchiveRecordAppendPackageMetadata>());
                var message = harness.Sent.Select<IArchiveRecordAppendPackageMetadata>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_with_no_metadata_access_tokens_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = "Aip@DossierId" },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string>() }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // Assert
                harvestManager.Verify(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>()), Times.Once);
            }
            finally
            {
                await harness.Stop();
            }

        }

        [Test]
        public async Task If_update_is_requested_with_removed_primary_data_package_then_cache_is_deleted()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = null },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord { ArchiveRecordId = archvieRecordId, PrimaryDataLink = "AIP@DossierId" }
            };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);
            findArchiveRecordClient.Setup(e => e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            // Act
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() => new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Sent.Any<IUpdateArchiveRecord>());
                var message = harness.Sent.Select<IUpdateArchiveRecord>().FirstOrDefault();

                // was the remove from cache message sent
                Assert.That(await harness.Sent.Any<IDeleteFileFromCache>());
                var message2 = harness.Sent.Select<IDeleteFileFromCache>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);

                // Verify the delete cache method was called
                Assert.That(message2 != null);
                message2.Context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_with_removed_primary_data_package_and_no_existing_elastic_record_normal_update_is_made()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata { PrimaryDataLink = null },
                Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string> { "Ö1" } }
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse { ArchiveRecordId = archvieRecordId, ElasticArchiveRecord = null };
            var response = new Mock<Response<FindArchiveRecordResponse>>();
            response.Setup(r => r.Message).Returns(findResult);
            findArchiveRecordClient.Setup(e =>
                    e.GetResponse<FindArchiveRecordResponse>(It.IsAny<FindArchiveRecordRequest>(), It.IsAny<CancellationToken>(),
                        It.IsAny<RequestTimeout>()))
                .Returns(Task.FromResult(response.Object));

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() =>
                new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // was the update ArchiveRecord message sent
                Assert.That(await harness.Sent.Any<IUpdateArchiveRecord>());
                var message = harness.Sent.Select<IUpdateArchiveRecord>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
                message.Context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }
        }


        [Test]
        public async Task If_update_is_requested_for_inexisting_record_the_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(() => null);
            harvestManager.Setup(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>())).Verifiable();

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() =>
                new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // Assert
                harvestManager.Verify(
                    v => v.UpdateMutationStatus(It.Is<MutationStatusInfo>(m =>
                        m.ChangeFromStatus == ActionStatus.SyncInProgress && m.NewStatus == ActionStatus.SyncAborted && m.MutationId == mutationId)),
                    Times.Once);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_update_is_requested_for_forbidden_archive_record_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            var ar = new ArchiveRecord
            { ArchiveRecordId = archvieRecordId, Security = new ArchiveRecordSecurity { MetadataAccessToken = new List<string>() } };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            harvestManager.Setup(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>())).Verifiable();

            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer(() =>
                new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object));

            await harness.Start();
            try
            {
                // Act
                await harness.InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
                {
                    ArchiveRecordId = archvieRecordId,
                    MutationId = mutationId,
                    Action = "UpDaTe"
                });

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ISyncArchiveRecord>());

                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ISyncArchiveRecord>());

                // Assert
                harvestManager.Verify(
                    v => v.UpdateMutationStatus(It.Is<MutationStatusInfo>(m =>
                        m.ChangeFromStatus == ActionStatus.SyncInProgress && m.NewStatus == ActionStatus.SyncAborted && m.MutationId == mutationId)),
                    Times.Once);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}
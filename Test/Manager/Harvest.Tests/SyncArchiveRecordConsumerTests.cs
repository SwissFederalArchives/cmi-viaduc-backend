using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Consumers;
using CMI.Manager.Harvest.Infrastructure;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Harvest.Tests
{
    public class SyncArchiveRecordConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<ICachedHarvesterSetting> cachedHarvesterSetting = new Mock<ICachedHarvesterSetting>();

        private readonly Mock<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>> findArchiveRecordClient =
            new Mock<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>();

        private readonly Mock<IHarvestManager> harvestManager = new Mock<IHarvestManager>();
        private readonly Mock<IConsumer<ISyncArchiveRecord>> syncArchiveRecordConsumer = new Mock<IConsumer<ISyncArchiveRecord>>();
        private Task<ConsumeContext<IArchiveRecordAppendPackageMetadata>> appendPackageMetadataTask;
        private Task<ConsumeContext<IArchiveRecordAppendPackage>> appendPackageTask;
        private Task<ConsumeContext<IRemoveArchiveRecord>> removeArchiveRemoveTask;
        private Task<ConsumeContext<IDeleteFileFromCache>> removeFileFromCacheTask;
        private Task<ConsumeContext<ISyncArchiveRecord>> syncArchiveRecordTask;
        private Task<ConsumeContext<IUpdateArchiveRecord>> updateArchiveRecordTask;

        public SyncArchiveRecordConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            harvestManager.Reset();
            syncArchiveRecordConsumer.Reset();
            cachedHarvesterSetting.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            cachedHarvesterSetting.Setup(s => s.EnableFullResync()).Returns(false);
            syncArchiveRecordTask = Handler<ISyncArchiveRecord>(configurator,
                context =>
                    new SyncArchiveRecordConsumer(harvestManager.Object, findArchiveRecordClient.Object, cachedHarvesterSetting.Object)
                        .Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.IndexManagerUpdateArchiveRecordMessageQueue, ec =>
            {
                ec.Consumer(() => syncArchiveRecordConsumer.Object);
                updateArchiveRecordTask = Handled<IUpdateArchiveRecord>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.IndexManagerRemoveArchiveRecordMessageQueue, ec =>
            {
                ec.Consumer(() => syncArchiveRecordConsumer.Object);
                removeArchiveRemoveTask = Handled<IRemoveArchiveRecord>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue, ec =>
            {
                ec.Consumer(() => syncArchiveRecordConsumer.Object);
                appendPackageTask = Handled<IArchiveRecordAppendPackage>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.RepositoryManagerReadPackageMetadataMessageQueue, ec =>
            {
                ec.Consumer(() => syncArchiveRecordConsumer.Object);
                appendPackageMetadataTask = Handled<IArchiveRecordAppendPackageMetadata>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.CacheDeleteFile, ec =>
            {
                ec.Consumer(() => syncArchiveRecordConsumer.Object);
                removeFileFromCacheTask = Handled<IDeleteFileFromCache>(ec);
            });
        }

        [Test]
        public async Task If_delete_is_requested_record_is_removed_from_index()
        {
            // Arrange
            var archvieRecordId = "34599";
            var mutationId = 6616;
            var ar = new ArchiveRecord {ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = null}};
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);


            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "delete"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await removeArchiveRemoveTask;

            // Assert
            context.Message.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }

        [Test]
        public async Task If_update_is_requested_with_no_primary_data_index_is_updated()
        {
            // Arrange
            var archvieRecordId = "345";
            var mutationId = 666;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = null},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord {ArchiveRecordId = archvieRecordId, PrimaryDataLink = null}
            };
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await updateArchiveRecordTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }

        [Test]
        public async Task If_update_is_requested_with_primary_data_and_elastic_record_with_no_primaryLink_package_metadata_is_appended()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = "Aip@DossierId"},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord {ArchiveRecordId = archvieRecordId, PrimaryDataLink = null}
            };
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await appendPackageMetadataTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }

        [Test]
        public async Task If_update_is_requested_with_primary_data_and_elastic_record_with_different_primaryLink_package_metadata_is_appended()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = "Aip@DossierId"},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord {ArchiveRecordId = archvieRecordId, PrimaryDataLink = "DifferentAip@DossierId"}
            };
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await appendPackageMetadataTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
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
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = "Aip@DossierId"},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
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
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await updateArchiveRecordTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.ArchiveRecord.ElasticPrimaryData[0].FileCount.Should().Be(5);
            context.Message.ArchiveRecord.ElasticPrimaryData[0].PackageId.Should().Be("controlPackageId");
            context.Message.MutationId.Should().Be(mutationId);
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
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = "Aip@DossierId"},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
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
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await appendPackageMetadataTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }

        [Test]
        public async Task If_update_is_requested_with_no_metadata_access_tokens_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = "Aip@DossierId"},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string>()}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;

            // Assert
            harvestManager.Verify(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>()), Times.Once);
        }

        [Test]
        public async Task If_update_is_requested_with_removed_primary_data_package_then_cache_is_deleted()
        {
            // Arrange
            var archvieRecordId = "3457";
            var mutationId = 6626;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = null},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse
            {
                ArchiveRecordId = archvieRecordId,
                ElasticArchiveRecord = new ElasticArchiveRecord {ArchiveRecordId = archvieRecordId, PrimaryDataLink = "AIP@DossierId"}
            };
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await updateArchiveRecordTask;
            var context2 = await removeFileFromCacheTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);

            // Verify the delete cache method was called
            context2.Message.ArchiveRecordId.Should().Be(archvieRecordId);
        }

        [Test]
        public async Task If_update_is_requested_with_removed_primary_data_package_and_no_existing_elastic_record_normal_update_is_made()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archvieRecordId, Metadata = new ArchiveRecordMetadata {PrimaryDataLink = null},
                Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string> {"Ö1"}}
            };
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            var findResult = new FindArchiveRecordResponse {ArchiveRecordId = archvieRecordId, ElasticArchiveRecord = null};
            findArchiveRecordClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(findResult));

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;
            var context = await updateArchiveRecordTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(archvieRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }

        [Test]
        public async Task If_update_is_requested_for_inexisting_record_the_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(() => null);
            harvestManager.Setup(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>())).Verifiable();

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;

            // Assert
            harvestManager.Verify(
                v => v.UpdateMutationStatus(It.Is<MutationStatusInfo>(m =>
                    m.ChangeFromStatus == ActionStatus.SyncInProgress && m.NewStatus == ActionStatus.SyncAborted && m.MutationId == mutationId)),
                Times.Once);
        }

        [Test]
        public async Task If_update_is_requested_for_forbidden_archive_record_sync_is_aborted()
        {
            // Arrange
            var archvieRecordId = "34527";
            var mutationId = 66267;
            var ar = new ArchiveRecord
                {ArchiveRecordId = archvieRecordId, Security = new ArchiveRecordSecurity {MetadataAccessToken = new List<string>()}};
            harvestManager.Setup(e => e.BuildArchiveRecord(archvieRecordId)).Returns(ar);
            harvestManager.Setup(e => e.UpdateMutationStatus(It.IsAny<MutationStatusInfo>())).Verifiable();

            // Act
            await InputQueueSendEndpoint.Send<ISyncArchiveRecord>(new
            {
                ArchiveRecordId = archvieRecordId,
                MutationId = mutationId,
                Action = "UpDaTe"
            });

            // Wait for the results
            await syncArchiveRecordTask;

            // Assert
            harvestManager.Verify(
                v => v.UpdateMutationStatus(It.Is<MutationStatusInfo>(m =>
                    m.ChangeFromStatus == ActionStatus.SyncInProgress && m.NewStatus == ActionStatus.SyncAborted && m.MutationId == mutationId)),
                Times.Once);
        }
    }
}
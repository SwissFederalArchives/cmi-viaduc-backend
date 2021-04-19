using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Repository.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    public class ReadPackageMetadataConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IArchiveRecordAppendPackageMetadata>> readPackageMetadataConsumer =
            new Mock<IConsumer<IArchiveRecordAppendPackageMetadata>>();

        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();
        private Task<ConsumeContext<IArchiveRecordUpdated>> archiveRecordUpdatedTask;
        private Task<ConsumeContext<IArchiveRecordAppendPackageMetadata>> readPackageMetadataTask;
        private Task<ConsumeContext<IScheduleForPackageSync>> scheduleForPackageSyncTask;

        public ReadPackageMetadataConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            repositoryManager.Reset();
            readPackageMetadataConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            readPackageMetadataTask = Handler<IArchiveRecordAppendPackageMetadata>(configurator,
                context => new ReadPackageMetadataConsumer(repositoryManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.AssetManagerSchdeduleForPackageSyncMessageQueue, ec =>
            {
                ec.Consumer(() => readPackageMetadataConsumer.Object);
                scheduleForPackageSyncTask = Handled<IScheduleForPackageSync>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
            {
                ec.Consumer(() => readPackageMetadataConsumer.Object);
                archiveRecordUpdatedTask = Handled<IArchiveRecordUpdated>(ec);
            });
        }

        [Test]
        public async Task If_AppendPackageMetadata_failed_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "345"};
            var mutationId = 666;
            var errMsg = "Some error message";
            var appendResult = new RepositoryPackageInfoResult {Valid = false, Success = false, ErrorMessage = errMsg};
            repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await readPackageMetadataTask;
            var context = await archiveRecordUpdatedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(errMsg);
        }

        [Test]
        public async Task If_Package_not_valid_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "344"};
            var mutationId = 999;
            var errMsg = "Some other error message";
            var appendResult = new RepositoryPackageInfoResult {Valid = false, Success = true, ErrorMessage = errMsg};
            repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await readPackageMetadataTask;
            var context = await archiveRecordUpdatedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(errMsg);
        }

        [Test]
        public async Task If_Package_is_valid_the_package_is_scheduled_for_sync()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "478"};
            var mutationId = 777;
            var errMsg = string.Empty;
            var appendResult = new RepositoryPackageInfoResult
                {Valid = true, Success = true, ErrorMessage = errMsg, PackageDetails = new RepositoryPackage {ArchiveRecordId = ar.ArchiveRecordId}};
            repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await readPackageMetadataTask;
            var context = await scheduleForPackageSyncTask;

            // Assert
            context.Message.Workload.ArchiveRecord.ArchiveRecordId.Should().Be(ar.ArchiveRecordId);
            context.Message.Workload.MutationId.Should().Be(mutationId);
        }
    }
}
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
    public class AppendPackageConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IArchiveRecordAppendPackage>> archiveRecordAppendPackageConsumer =
            new Mock<IConsumer<IArchiveRecordAppendPackage>>();

        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();
        private Task<ConsumeContext<IArchiveRecordAppendPackage>> appendPackageTask;
        private Task<ConsumeContext<IArchiveRecordUpdated>> archiveRecordUpdatedTask;
        private Task<ConsumeContext<IArchiveRecordExtractFulltextFromPackage>> extractFulltextTask;

        public AppendPackageConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            repositoryManager.Reset();
            archiveRecordAppendPackageConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            appendPackageTask = Handler<IArchiveRecordAppendPackage>(configurator,
                context => new AppendPackageConsumer(repositoryManager.Object).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.AssetManagerExtractFulltextMessageQueue, ec =>
            {
                ec.Consumer(() => archiveRecordAppendPackageConsumer.Object);
                extractFulltextTask = Handled<IArchiveRecordExtractFulltextFromPackage>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
            {
                ec.Consumer(() => archiveRecordAppendPackageConsumer.Object);
                archiveRecordUpdatedTask = Handled<IArchiveRecordUpdated>(ec);
            });
        }

        [Test]
        public async Task If_AppendPackage_failed_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "345"};
            var mutationId = 666;
            var errMsg = "Some error message";
            var appendResult = new RepositoryPackageResult {Valid = false, Success = false, ErrorMessage = errMsg};
            repositoryManager.Setup(e => e.AppendPackageToArchiveRecord(It.IsAny<ArchiveRecord>(), It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await appendPackageTask;
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
            var appendResult = new RepositoryPackageResult {Valid = false, Success = true, ErrorMessage = errMsg};
            repositoryManager.Setup(e => e.AppendPackageToArchiveRecord(It.IsAny<ArchiveRecord>(), It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await appendPackageTask;
            var context = await archiveRecordUpdatedTask;

            // Assert
            context.Message.ActionSuccessful.Should().Be(false);
            context.Message.MutationId.Should().Be(mutationId);
            context.Message.ErrorMessage.Should().Be(errMsg);
        }

        [Test]
        public async Task If_Package_is_valid_extract_fulltext_is_initiated()
        {
            // Arrange
            var ar = new ArchiveRecord {ArchiveRecordId = "478"};
            var mutationId = 777;
            var errMsg = string.Empty;
            var appendResult = new RepositoryPackageResult
            {
                Valid = true, Success = true, ErrorMessage = errMsg, PackageDetails = new RepositoryPackage
                {
                    PackageFileName = "need a file name.whatever"
                }
            };
            repositoryManager.Setup(e => e.AppendPackageToArchiveRecord(It.IsAny<ArchiveRecord>(), It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(appendResult);

            // Act
            await InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
            {
                ArchiveRecord = ar,
                MutationId = mutationId
            });

            // Wait for the results
            await appendPackageTask;
            var context = await extractFulltextTask;

            // Assert
            context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(ar.ArchiveRecordId);
            context.Message.MutationId.Should().Be(mutationId);
        }
    }
}
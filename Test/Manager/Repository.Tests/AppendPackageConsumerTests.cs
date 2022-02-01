using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Repository.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    public class AppendPackageConsumerTests
    {
        private readonly Mock<IConsumer<IArchiveRecordAppendPackage>> archiveRecordAppendPackageConsumer =
            new Mock<IConsumer<IArchiveRecordAppendPackage>>();

        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();

        private InMemoryTestHarness harness;

        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            repositoryManager.Reset();
            archiveRecordAppendPackageConsumer.Reset();
        }
        [Test]
        public async Task If_AppendPackage_failed_Sync_process_is_set_to_failed()
        {
            // Arrange
            try
            {
                
                var ar = new ArchiveRecord { ArchiveRecordId = "345" };
                var mutationId = 666;
                var errMsg = "Some error message";
                var appendResult = new RepositoryPackageResult { Valid = false, Success = false, ErrorMessage = errMsg };
                repositoryManager.Setup(e => e.AppendPackageToArchiveRecord(It.IsAny<ArchiveRecord>(), It.IsAny<long>(), It.IsAny<int>()))
                    .ReturnsAsync(appendResult);

                var appendPackageConsumer = harness.Consumer(() => new AppendPackageConsumer(repositoryManager.Object));
                harness.Consumer(() => archiveRecordAppendPackageConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // Wait for the results

                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackage>());
                Assert.That(await appendPackageConsumer.Consumed.Any<IArchiveRecordAppendPackage>());

                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var context = harness.Published.Select<IArchiveRecordUpdated>().First().Context;

                // Assert
                context.Message.ActionSuccessful.Should().Be(false);
                context.Message.MutationId.Should().Be(mutationId);
                context.Message.ErrorMessage.Should().Be(errMsg);
            }
            finally
            {
                await harness.Stop();
            }


        }

        [Test]
        public async Task If_Package_not_valid_Sync_process_is_set_to_failed()
        {
            // Arrange
            try
            {
                var ar = new ArchiveRecord { ArchiveRecordId = "344" };
                var mutationId = 999;
                var errMsg = "Some other error message";
                var appendResult = new RepositoryPackageResult { Valid = false, Success = true, ErrorMessage = errMsg };
                repositoryManager.Setup(e => e.AppendPackageToArchiveRecord(It.IsAny<ArchiveRecord>(), It.IsAny<long>(), It.IsAny<int>()))
                    .ReturnsAsync(appendResult);

                var appendPackageConsumer = harness.Consumer(() => new AppendPackageConsumer(repositoryManager.Object));
                harness.Consumer(() => archiveRecordAppendPackageConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackage>());
                Assert.That(await appendPackageConsumer.Consumed.Any<IArchiveRecordAppendPackage>());
                
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var context = harness.Published.Select<IArchiveRecordUpdated>().First().Context;

                // Assert
                context.Message.ActionSuccessful.Should().Be(false);
                context.Message.MutationId.Should().Be(mutationId);
                context.Message.ErrorMessage.Should().Be(errMsg);

            }
            finally
            {
                await harness.Stop();
            }

        }

        [Test]
        public async Task If_Package_is_valid_preprocessing_of_asset_is_initiated()
        {
            try
            {
                // Arrange
                var ar = new ArchiveRecord { ArchiveRecordId = "478" };
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

                var appendPackageConsumer = harness.Consumer(() => new AppendPackageConsumer(repositoryManager.Object));
                harness.Consumer(() => archiveRecordAppendPackageConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackage>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackage>());
                Assert.That(await appendPackageConsumer.Consumed.Any<IArchiveRecordAppendPackage>());

                Assert.That(await harness.Sent.Any<PrepareForRecognitionMessage>());
                var context = harness.Sent.Select<PrepareForRecognitionMessage>().First().Context;

                // Assert
                context.Message.ArchiveRecord.ArchiveRecordId.Should().Be(ar.ArchiveRecordId);
                context.Message.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }

        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Repository.Consumer;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    public class ReadPackageMetadataConsumerTests
    {
        private readonly Mock<IConsumer<IArchiveRecordAppendPackageMetadata>> readPackageMetadataConsumer = new Mock<IConsumer<IArchiveRecordAppendPackageMetadata>>();

        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();

        [SetUp]
        public void Setup()
        {
            repositoryManager.Reset();
            readPackageMetadataConsumer.Reset();
        }
        
        [Test]
        public async Task If_AppendPackageMetadata_failed_Sync_process_is_set_to_failed()
        {
            // Arrange
            var harness = new InMemoryTestHarness();
            try
            {
                var ar = new ArchiveRecord { ArchiveRecordId = "345" };
                var mutationId = 666;
                var errMsg = "Some error message";
                var appendResult = new RepositoryPackageInfoResult { Valid = false, Success = false, ErrorMessage = errMsg };
                repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

                var readMetadataConsumer = harness.Consumer(() => readPackageMetadataConsumer.Object);
                harness.Consumer(() => new ReadPackageMetadataConsumer(repositoryManager.Object));
                harness.Consumer(() => readPackageMetadataConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackageMetadata>());

                // did the actual consumer consume the message
                Assert.That(await readMetadataConsumer.Consumed.Any<IArchiveRecordAppendPackageMetadata>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());

                // ensure that no faults were published by the consumer
                Assert.That(await harness.Published.Any<Fault<IArchiveRecordUpdated>>(), Is.False);

                // did the actual consumer consume the message
                var message = harness.Published.Select<IArchiveRecordUpdated>().First().Context.Message;

                Assert.That(message != null);
                message.ActionSuccessful.Should().Be(false);
                message.MutationId.Should().Be(mutationId);
                message.ErrorMessage.Should().Be(errMsg);
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
            var harness = new InMemoryTestHarness();
            try
            {
                var ar = new ArchiveRecord { ArchiveRecordId = "344" };
                var mutationId = 999;
                var errMsg = "Some other error message";
                var appendResult = new RepositoryPackageInfoResult { Valid = false, Success = true, ErrorMessage = errMsg };
                repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

                var readMetadataConsumer = harness.Consumer(() => readPackageMetadataConsumer.Object);
                harness.Consumer(() => new ReadPackageMetadataConsumer(repositoryManager.Object));
                harness.Consumer(() => readPackageMetadataConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });


                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackageMetadata>());

                // did the actual consumer consume the message
                Assert.That(await readMetadataConsumer.Consumed.Any<IArchiveRecordAppendPackageMetadata>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());

                // ensure that no faults were published by the consumer
                Assert.That(await harness.Published.Any<Fault<IArchiveRecordUpdated>>(), Is.False);

                // did the actual consumer consume the message
                var message = harness.Published.Select<IArchiveRecordUpdated>().First().Context.Message;

                // Assert
                message.ActionSuccessful.Should().Be(false);
                message.MutationId.Should().Be(mutationId);
                message.ErrorMessage.Should().Be(errMsg);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_Package_is_valid_the_package_is_scheduled_for_sync()
        {
            // Arrange
            var harness = new InMemoryTestHarness();
            try
            {
                var ar = new ArchiveRecord { ArchiveRecordId = "478" };
                var mutationId = 777;
                var errMsg = string.Empty;
                var appendResult = new RepositoryPackageInfoResult
                    { Valid = true, Success = true, ErrorMessage = errMsg, PackageDetails = new RepositoryPackage { ArchiveRecordId = ar.ArchiveRecordId } };
                repositoryManager.Setup(e => e.ReadPackageMetadata(It.IsAny<string>(), It.IsAny<string>())).Returns(appendResult);

                var readMetadataConsumer = harness.Consumer(() => readPackageMetadataConsumer.Object);
                harness.Consumer(() => new ReadPackageMetadataConsumer(repositoryManager.Object));
                harness.Consumer(() => readPackageMetadataConsumer.Object);

                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send<IArchiveRecordAppendPackageMetadata>(new
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId
                });

                // Assert
                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IArchiveRecordAppendPackageMetadata>());

                // did the actual consumer consume the message
                Assert.That(await readMetadataConsumer.Consumed.Any<IArchiveRecordAppendPackageMetadata>());
                
                Assert.That(await harness.Sent.Any<IScheduleForPackageSync>());
                var message = harness.Sent.Select<IScheduleForPackageSync>().First().Context.Message;

                // Assert
                message.Workload.ArchiveRecord.ArchiveRecordId.Should().Be(ar.ArchiveRecordId);
                message.Workload.MutationId.Should().Be(mutationId);
            }
            finally
            {
                await harness.Stop();
            }

        }
    }
}
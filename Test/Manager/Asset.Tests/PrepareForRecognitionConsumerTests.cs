using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Asset;
using CMI.Manager.Asset.Consumers;
using FluentAssertions;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class PrepareForRecognitionConsumerTests
    {
        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<IAssetPreparationEngine> preparationEngine = new Mock<IAssetPreparationEngine>();
        private InMemoryTestHarness harness;

        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            assetManager.Reset();
            preparationEngine.Reset();
        }
        [Test]
        public async Task If_PrepareForRecognition_failed_Sync_process_is_set_to_failed()
        {
            try
            {
                // Arrange
                var ar = new ArchiveRecord
                {
                    ArchiveRecordId = "478", PrimaryData = null // This provokes a failure
                };
                var mutationId = 777;

                var consumer = harness.Consumer(() => new PrepareForRecognitionConsumer(assetManager.Object, preparationEngine.Object));
                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send(new PrepareForRecognitionMessage
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,
                    PrimaerdatenAuftragId = 458
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<PrepareForRecognitionMessage>());
                Assert.That(await consumer.Consumed.Any<PrepareForRecognitionMessage>());

                Assert.That(await harness.Published.Any<IArchiveRecordUpdated>());
                var context = harness.Published.Select<IArchiveRecordUpdated>().First().Context;

                // Assert
                context.Message.ArchiveRecordId.ToString().Should().Be(ar.ArchiveRecordId);
                context.Message.MutationId.Should().Be(mutationId);
                context.Message.ActionSuccessful.Should().BeFalse();
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_PrepareForRecognition_is_valid_extract_fulltext_is_initiated()
        {
            try
            {
                // Arrange
                var ar = new ArchiveRecord { ArchiveRecordId = "478", PrimaryData = new List<RepositoryPackage>
                {
                    new RepositoryPackage {PackageFileName = "Testdummy", ArchiveRecordId = "478", Files = new List<RepositoryFile>{new RepositoryFile {PhysicalName = "test.xml"}}}
                }};
                var mutationId = 777;

                assetManager.Setup(s => s.ExtractZipFile(It.IsAny<ExtractZipArgument>())).Returns(() => Task.FromResult(true));

                var consumer = harness.Consumer(() => new PrepareForRecognitionConsumer(assetManager.Object, preparationEngine.Object));
                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send(new PrepareForRecognitionMessage
                {
                    ArchiveRecord = ar,
                    MutationId = mutationId,
                    PrimaerdatenAuftragId = 458
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<PrepareForRecognitionMessage>());
                Assert.That(await consumer.Consumed.Any<PrepareForRecognitionMessage>());

                Assert.That(await harness.Sent.Any<IArchiveRecordExtractFulltextFromPackage>());
                var context = harness.Sent.Select<IArchiveRecordExtractFulltextFromPackage>().First().Context;

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
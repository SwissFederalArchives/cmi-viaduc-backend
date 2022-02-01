using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Manager.Asset.Consumers;
using FluentAssertions;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class PrepareForTransformationConsumerTests
    {
        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<IAssetPreparationEngine> preparationEngine = new Mock<IAssetPreparationEngine>();
        private readonly Mock<IScanProcessor> scanProcessorMock = new Mock<IScanProcessor>();
        private readonly Mock<ITransformEngine> transformEngineMock = new Mock<ITransformEngine>();
        private InMemoryTestHarness harness;
        private RepositoryPackage repositoryPackage;

        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            assetManager.Reset();
            preparationEngine.Reset();
            scanProcessorMock.Reset();
            transformEngineMock.Reset();
            repositoryPackage = new RepositoryPackage { PackageFileName = "testfile.zip", ArchiveRecordId = "112" };
        }
        [Test]
        public async Task If_PrepareForTransformation_failed_Sync_process_is_set_to_failed()
        {
            try
            {
                // Arrange
                var ar = new ArchiveRecord
                {
                    ArchiveRecordId = "112", PrimaryData = null // This provokes a failure
                };
                var orderId = 777;


                var consumer = harness.Consumer(() => new PrepareForTransformationConsumer(assetManager.Object, scanProcessorMock.Object, transformEngineMock.Object, 
                    preparationEngine.Object));
                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send(new PrepareForTransformationMessage()
                {
                    AssetType = AssetType.Gebrauchskopie,
                    RepositoryPackage = repositoryPackage,
                    CallerId = "123",
                    OrderItemId = orderId,
                    ProtectWithPassword = false,
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic,
                    PrimaerdatenAuftragId = 458
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<PrepareForTransformationMessage>());
                Assert.That(await consumer.Consumed.Any<PrepareForTransformationMessage>());

                Assert.That(await harness.Published.Any<IAssetReady>());
                var context = harness.Published.Select<IAssetReady>().First().Context;

                // Assert
                context.Message.ArchiveRecordId.Should().Be(ar.ArchiveRecordId);
                context.Message.OrderItemId.Should().Be(orderId);
                context.Message.Valid.Should().BeFalse();
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_PrepareForTransformation_is_valid_Transformation_is_initiated()
        {
            try
            {
                // Arrange
                repositoryPackage.ArchiveRecordId = "112";
                var orderId = 777;

                assetManager.Setup(s => s.ExtractZipFile(It.IsAny<ExtractZipArgument>())).Returns(() => Task.FromResult(true));

                var consumer = harness.Consumer(() => new PrepareForTransformationConsumer(assetManager.Object, scanProcessorMock.Object, transformEngineMock.Object,
                    preparationEngine.Object));
                await harness.Start();

                // Act
                await harness.InputQueueSendEndpoint.Send(new PrepareForTransformationMessage()
                {
                    AssetType = AssetType.Gebrauchskopie,
                    RepositoryPackage = repositoryPackage,
                    CallerId = "123",
                    OrderItemId = orderId,
                    ProtectWithPassword = false,
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic,
                    PrimaerdatenAuftragId = 458
                });

                // Wait for the results
                Assert.That(await harness.Consumed.Any<PrepareForTransformationMessage>());
                Assert.That(await consumer.Consumed.Any<PrepareForTransformationMessage>());

                Assert.That(await harness.Sent.Any<ITransformAsset>());
                var context = harness.Sent.Select<ITransformAsset>().First().Context;

                // Assert
                context.Message.RepositoryPackage.ArchiveRecordId.Should().Be("112");
                context.Message.OrderItemId.Should().Be(orderId);
                context.Message.PrimaerdatenAuftragId = 458;
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Asset;
using CMI.Engine.Asset.PreProcess;
using CMI.Manager.Asset.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class PrepareForTransformationConsumerTests
    {
        private Mock<IAssetManager> assetManager;
        private Mock<IAssetPreparationEngine> preparationEngine;
        private Mock<IScanProcessor> scanProcessorMock;
        private Mock<ITransformEngine> transformEngineMock;
        private RepositoryPackage repositoryPackage;
        private ITestHarness harness;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            preparationEngine = new Mock<IAssetPreparationEngine>();
            scanProcessorMock = new Mock<IScanProcessor>();
            transformEngineMock = new Mock<ITransformEngine>();
            repositoryPackage = new RepositoryPackage { PackageFileName = "testfile.zip", ArchiveRecordId = "112" };

            // Build the container
            provider = new ServiceCollection().AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<PrepareForTransformationConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerPrepareForTransformation);
                    cfg.AddConsumer<TransformPackageConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerTransformAssetMessageQueue);
                    cfg.AddConsumer<PrepareForRecognitionConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerPrepareForRecognition);
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => preparationEngine.Object);
                    cfg.AddTransient(_ => scanProcessorMock.Object);
                    cfg.AddTransient(_ => transformEngineMock.Object);
                    cfg.AddTransient(_ => repositoryPackage); 

                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>();
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

                await harness.Start();
                // Act
                await harness.Bus.Publish(new PrepareForTransformationMessage
                {
                    AssetType = AssetType.Gebrauchskopie,
                    RepositoryPackage = repositoryPackage,
                    CallerId = "123",
                    OrderItemId = orderId,
                    ProtectWithPassword = false,
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic,
                    PrimaerdatenAuftragId = 458
                });


                // Assert
                // Wait for the results
                Assert.IsTrue(await harness.Published.Any<PrepareForTransformationMessage>());
                var consumerHarness = harness.GetConsumerHarness<PrepareForTransformationConsumer>();
                Assert.That(await consumerHarness.Consumed.Any<PrepareForTransformationMessage>());
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

                await harness.Start();

                // Act
                await harness.Bus.Publish(new PrepareForTransformationMessage
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
                Assert.IsTrue(await harness.Published.Any<PrepareForTransformationMessage>());
                var consumer = harness.GetConsumerHarness<PrepareForTransformationConsumer>();
                Assert.That(await consumer.Consumed.Any<PrepareForTransformationMessage>());
                var context = harness.Published.Select<ITransformAsset>().First().Context;

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
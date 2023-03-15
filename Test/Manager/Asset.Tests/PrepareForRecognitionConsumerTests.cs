using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Asset;
using CMI.Engine.Asset.PreProcess;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;


namespace CMI.Manager.Asset.Tests
{
    public class PrepareForRecognitionConsumerTests
    {
        private Mock<IAssetManager> assetManager;
        private Mock<IAssetPreparationEngine> preparationEngine;
        private ITestHarness harness;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            preparationEngine = new Mock<IAssetPreparationEngine>();

            // Build the container
            provider = new ServiceCollection().AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<PrepareForRecognitionConsumer>(); 
                    cfg.AddConsumer<ExtractFulltextPackageConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerExtractFulltextMessageQueue);
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => preparationEngine.Object);
                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>(); ;
        }

        [Test]
        public async Task If_PrepareForRecognition_failed_Sync_process_is_set_to_failed()
        {
            // Arrange
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "478", PrimaryData = null // This provokes a failure
            };
            var mutationId = 777;
            await harness.Start();
            var client = harness.GetRequestClient<PrepareForRecognitionMessage>();

            // Act && Assert
            Assert.ThrowsAsync<RequestFaultException>(async () => await client.GetResponse<PrepareForRecognitionMessage>(new PrepareForRecognitionMessage
            {
                ArchiveRecord = ar,
                MutationId = mutationId,
                PrimaerdatenAuftragId = 25
            }));
            await harness.Stop();
        }

        [Test]
        public async Task If_PrepareForRecognition_is_valid_extract_fulltext_is_initiated()
        {
            // Arrange
            assetManager.Reset();
            var ar = new ArchiveRecord
            {
                ArchiveRecordId = "478", PrimaryData = new List<RepositoryPackage>
                {
                    new() {PackageFileName = "Testdummy.pdf", ArchiveRecordId = "478", Files = new List<RepositoryFile>{new RepositoryFile {PhysicalName = "test.xml"}}}
                }
            };
            var mutationId = 777;

            assetManager.Setup(s => s.ExtractZipFile(It.IsAny<ExtractZipArgument>())).Returns(() => Task.FromResult(true));
            await harness.Start();

            // Act
            await harness.Bus.Publish(new PrepareForRecognitionMessage
            {
                ArchiveRecord = ar,
                MutationId = mutationId,
                PrimaerdatenAuftragId = 458
            });

            // Assert
            Assert.IsTrue(await harness.Published.Any<PrepareForRecognitionMessage>());
            var consumerHarness = harness.GetConsumerHarness<ExtractFulltextPackageConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IArchiveRecordExtractFulltextFromPackage>());
            // Zeitabhängig
            assetManager.Verify(a => a.UpdatePrimaerdatenAuftragStatus(It.IsAny<UpdatePrimaerdatenAuftragStatus>()), Times.Between(1, 3, Range.Inclusive));
            await harness.Stop();
        }
    }
}
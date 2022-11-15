using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Consumers;
using CMI.Utilities.Cache.Access;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class TransformPackageConsumerTests
    {
        private Mock<IAssetManager> assetManager ;
        private Mock<ICacheHelper> cacheHelper;
        private RepositoryPackage repositoryPackage;
        private ITestHarness harness;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            cacheHelper = new Mock<ICacheHelper>();
            repositoryPackage = new RepositoryPackage {PackageFileName = "testfile.zip", ArchiveRecordId = "112"};
            // Build the container
            provider = new ServiceCollection().AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<TransformPackageConsumer>();
                    cfg.AddConsumer<PrepareForRecognitionConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerPrepareForRecognition);
                    cfg.AddConsumer<AssetReadyConsumer>().Endpoint(e => e.Name = BusConstants.AssetManagerAssetReadyEventQueue);
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => cacheHelper.Object);

                })
                .BuildServiceProvider(true);

            harness = provider.GetRequiredService<ITestHarness>(); ;
        }
        
        [Test]
        public async Task If_transformation_failed_AssetReady_event_is_no_success()
        {
            // Arrange
            assetManager.Setup(e => e.ConvertPackage("112", AssetType.Gebrauchskopie, false, It.IsAny<RepositoryPackage>()))
                .ReturnsAsync(new PackageConversionResult { Valid = false, FileName = "112.zip" });
           
            await harness.Start();
            try
            {
                // Act
                await harness.Bus.Publish<ITransformAsset>(new TransformAsset
                {
                    AssetType = AssetType.Gebrauchskopie,
                    CallerId = "2222",
                    Recipient = "jon@doe.com",
                    RepositoryPackage = repositoryPackage,
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic
                });


                // did the endpoint consume the message
                Assert.IsTrue(await harness.Published.Any<ITransformAsset>());
                Assert.That(await harness.Consumed.Any<ITransformAsset>());

                var consumer = harness.GetConsumerHarness<TransformPackageConsumer>();
                // did the actual consumer consume the message
                Assert.That(await consumer.Consumed.Any<ITransformAsset>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IAssetReady>());

                // ensure that no faults were published by the consumer
                Assert.IsFalse(await consumer.Consumed.Any<Fault<IAssetReady>>());

                // did the actual consumer consume the message
                Assert.That(await harness.Consumed.Any<IAssetReady>());
                var message = harness.Consumed.Select<IAssetReady>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be("112");
                message.Context.Message.CallerId = "2222";
                message.Context.Message.Valid.Should().Be(false);

            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_transformation_success_AssetReady_event_is_success()
        {
            // Arrange
            repositoryPackage.ArchiveRecordId = "113";
            assetManager.Setup(e => e.ConvertPackage("113", AssetType.Gebrauchskopie, false, It.IsAny<RepositoryPackage>()))
                .ReturnsAsync(new PackageConversionResult {Valid = true, FileName = "113.zip"});
            cacheHelper.Setup(e => e.GetFtpUrl(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .ReturnsAsync("ftp://UsageCopyPublic:@someurl:9000/113");
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .ReturnsAsync(true);
           
            await harness.Start();
            try
            {
                // Act
                await harness.Bus.Publish<ITransformAsset>(new TransformAsset
                {
                    AssetType = AssetType.Gebrauchskopie,
                    RepositoryPackage = repositoryPackage,
                    CallerId = "2223",
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ITransformAsset>());

                // did the actual consumer consume the message

                var consumer = harness.GetConsumerHarness<TransformPackageConsumer>();
                Assert.That(await consumer.Consumed.Any<ITransformAsset>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IAssetReady>());

                // ensure that no faults were published by the consumer
                Assert.IsFalse(await consumer.Consumed.Any<Fault<IAssetReady>>());

                // did the actual consumer consume the message
                Assert.That(await harness.Consumed.Any<IAssetReady>());
                var message = harness.Consumed.Select<IAssetReady>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be("113");
                message.Context.Message.CallerId = "2223";
                message.Context.Message.Valid.Should().Be(true);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_transformation_success_but_cache_upload_fails_AssetReady_event_is_failed()
        {
            // Arrange
            repositoryPackage.ArchiveRecordId = "114";
            assetManager.Setup(e => e.ConvertPackage("114", AssetType.Gebrauchskopie, false, It.IsAny<RepositoryPackage>()))
                .ReturnsAsync(new PackageConversionResult {Valid = true, FileName = "114.zip"});
            cacheHelper.Setup(e => e.GetFtpUrl(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .ReturnsAsync("ftp://UsageCopyPublic:@someurl:9000/114");
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            await harness.Start();
            try
            {
                // Act
                await harness.Bus.Publish<ITransformAsset>(new TransformAsset
                {
                    AssetType = AssetType.Gebrauchskopie,
                    RepositoryPackage = repositoryPackage,
                    CallerId = "2224",
                    RetentionCategory = CacheRetentionCategory.UsageCopyPublic
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ITransformAsset>());

                // did the actual consumer consume the message

                var consumer = harness.GetConsumerHarness<TransformPackageConsumer>();
                Assert.That(await consumer.Consumed.Any<ITransformAsset>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IAssetReady>());

                // ensure that no faults were published by the consumer
                Assert.IsFalse(await consumer.Consumed.Any<Fault<IAssetReady>>());

                // did the actual consumer consume the message
                Assert.That(await harness.Consumed.Any<IAssetReady>());
                var message = harness.Consumed.Select<IAssetReady>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.ArchiveRecordId.Should().Be("114");
                message.Context.Message.CallerId = "2224";
                message.Context.Message.Valid.Should().Be(false);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task If_transformation_is_benutzungskopie_AssetReady_event_is_false_original_file_is_zipped()
        {
            // Arrange
            repositoryPackage.ArchiveRecordId = "115";
            assetManager.Setup(e => e.ConvertPackage("115", AssetType.Benutzungskopie, false, It.IsAny<RepositoryPackage>()))
                .ReturnsAsync(new PackageConversionResult {Valid = false, FileName = "115.zip"});
            assetManager.Setup(e => e.CreateZipFileWithPasswordFromFile(It.IsAny<string>(), "115", AssetType.Benutzungskopie))
                .Returns("myZippedFile");
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), CacheRetentionCategory.UsageCopyBenutzungskopie, "myZippedFile"))
                .ReturnsAsync(true);
            
            await harness.Start();
            try
            {
                // Act
                await harness.Bus.Publish<ITransformAsset>(new TransformAsset
                {
                    OrderItemId = 115,
                    RepositoryPackage = repositoryPackage,
                    AssetType = AssetType.Benutzungskopie,
                    CallerId = "2225",
                    RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie
                });


                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<ITransformAsset>());

                // did the actual consumer consume the message
                var consumer = harness.GetConsumerHarness<TransformPackageConsumer>();
                Assert.That(await consumer.Consumed.Any<ITransformAsset>());

                // the consumer publish the event
                Assert.That(await harness.Published.Any<IAssetReady>());

                // ensure that no faults were published by the consume
                Assert.IsFalse(await consumer.Consumed.Any<Fault<IAssetReady>>());

                // did the endpoint consume the message
                Assert.That(await harness.Consumed.Any<IAssetReady>());
                var message = harness.Consumed.Select<IAssetReady>().FirstOrDefault();

                // Assert
                Assert.That(message != null);
                message.Context.Message.OrderItemId.Should().Be(115);
                message.Context.Message.CallerId = "2225";
                message.Context.Message.Valid.Should().Be(false);
                assetManager.Verify(a => a.CreateZipFileWithPasswordFromFile(It.IsAny<string>(), "115", AssetType.Benutzungskopie), Times.Once);
                cacheHelper.Verify(c => c.SaveToCache(It.IsAny<IBus>(), CacheRetentionCategory.UsageCopyBenutzungskopie, "myZippedFile"), Times.Once);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}
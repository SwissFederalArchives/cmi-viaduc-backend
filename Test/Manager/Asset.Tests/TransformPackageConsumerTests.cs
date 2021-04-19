using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using CMI.Utilities.Cache.Access;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    public class TransformPackageConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<IConsumer<IAssetReady>> assetReadyConsumer = new Mock<IConsumer<IAssetReady>>();
        private readonly Mock<ICacheHelper> cacheHelper = new Mock<ICacheHelper>();
        private readonly Mock<PasswordHelper> passwordHelper = new Mock<PasswordHelper>("seed");
        private Task<ConsumeContext<IAssetReady>> assetReadyHandled;
        private Task<ConsumeContext<ITransformAsset>> transformAssetTask;

        public TransformPackageConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            assetManager.Reset();
            cacheHelper.Reset();
            assetReadyConsumer.Reset();
            passwordHelper.Reset();
        }


        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            transformAssetTask = Handler<ITransformAsset>(configurator,
                context => new TransformPackageConsumer(assetManager.Object, cacheHelper.Object, Bus).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.AssetManagerAssetReadyEventQueue, ec =>
            {
                ec.Consumer(() => assetReadyConsumer.Object);
                assetReadyHandled = Handled<IAssetReady>(ec);
            });
        }

        [Test]
        public async Task If_transformation_failed_AssetReady_event_is_no_success()
        {
            // Arrange
            assetManager.Setup(e => e.ConvertPackage("112", AssetType.Gebrauchskopie, false, "testfile.zip", null))
                .ReturnsAsync(new PackageConversionResult {Valid = false, FileName = "112.zip"});

            // Act
            await InputQueueSendEndpoint.Send<ITransformAsset>(new
            {
                ArchiveRecordId = "112",
                FileName = "testfile.zip",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "223",
                Recipient = "jon@doe.com",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await transformAssetTask;
            var context = await assetReadyHandled;

            // Assert
            context.Message.ArchiveRecordId.Should().Be("112");
            context.Message.CallerId = "223";
            context.Message.Valid.Should().Be(false);
        }


        [Test]
        public async Task If_transformation_success_AssetReady_event_is_success()
        {
            // Arrange
            assetManager.Setup(e => e.ConvertPackage("111", AssetType.Gebrauchskopie, false, "testfile.zip", null))
                .ReturnsAsync(new PackageConversionResult {Valid = true, FileName = "111.zip"});
            cacheHelper.Setup(e => e.GetFtpUrl(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .Returns(Task.FromResult("ftp://UsageCopyPublic:@someurl:9000/111"));
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await InputQueueSendEndpoint.Send<ITransformAsset>(new
            {
                ArchiveRecordId = "111",
                FileName = "testfile.zip",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "222",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await transformAssetTask;
            var assetReadyContext = await assetReadyHandled;

            // Assert
            assetReadyContext.Message.ArchiveRecordId.Should().Be("111");
            assetReadyContext.Message.Valid.Should().Be(true);
        }

        [Test]
        public async Task If_transformation_success_but_cache_upload_fails_AssetReady_event_is_failed()
        {
            // Arrange
            assetManager.Setup(e => e.ConvertPackage("111", AssetType.Gebrauchskopie, false, "testfile.zip", null))
                .ReturnsAsync(new PackageConversionResult {Valid = true, FileName = "111.zip"});
            cacheHelper.Setup(e => e.GetFtpUrl(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .Returns(Task.FromResult("ftp://UsageCopyPublic:@someurl:9000/111"));
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await InputQueueSendEndpoint.Send<ITransformAsset>(new
            {
                ArchiveRecordId = "111",
                FileName = "testfile.zip",
                AssetType = AssetType.Gebrauchskopie,
                CallerId = "222",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await transformAssetTask;
            var assetReadyContext = await assetReadyHandled;

            // Assert
            assetReadyContext.Message.ArchiveRecordId.Should().Be("111");
            assetReadyContext.Message.Valid.Should().Be(false);
        }

        [Test]
        public async Task If_transformation_is_benutzungskopie_AssetReady_event_is_false_original_file_is_zipped()
        {
            // Arrange
            assetManager.Setup(e => e.ConvertPackage("111", AssetType.Benutzungskopie, false, "testfile.zip", null))
                .ReturnsAsync(new PackageConversionResult {Valid = false, FileName = "111.zip"});
            assetManager.Setup(e => e.CreateZipFileWithPasswordFromFile(It.IsAny<string>(), "111", AssetType.Benutzungskopie))
                .Returns("myZippedFile");
            cacheHelper.Setup(e => e.SaveToCache(It.IsAny<IBus>(), CacheRetentionCategory.UsageCopyBenutzungskopie, "myZippedFile"))
                .Returns(Task.FromResult(true));
            // Act
            await InputQueueSendEndpoint.Send<ITransformAsset>(new
            {
                OrderItemId = 111,
                FileName = "testfile.zip",
                AssetType = AssetType.Benutzungskopie,
                CallerId = "222",
                RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie
            });

            // Wait for the results
            await transformAssetTask;
            var assetReadyContext = await assetReadyHandled;

            // Assert
            assetReadyContext.Message.OrderItemId.Should().Be(111);
            assetReadyContext.Message.Valid.Should().Be(false);
            assetManager.Verify(a => a.CreateZipFileWithPasswordFromFile(It.IsAny<string>(), "111", AssetType.Benutzungskopie), Times.Once);
            cacheHelper.Verify(c => c.SaveToCache(It.IsAny<IBus>(), CacheRetentionCategory.UsageCopyBenutzungskopie, "myZippedFile"), Times.Once);
        }
    }
}
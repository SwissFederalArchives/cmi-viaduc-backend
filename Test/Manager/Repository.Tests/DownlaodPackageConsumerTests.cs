using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
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
    public class DownlaodPackageConsumerTests : InMemoryTestFixture
    {
        private readonly Mock<IConsumer<IDownloadPackage>> downloadPackageConsumer = new Mock<IConsumer<IDownloadPackage>>();

        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();
        private Task<ConsumeContext<IAssetReady>> assetReadyUpdatedTask;
        private Task<ConsumeContext<IDownloadPackage>> downloadPackageTask;
        private Task<ConsumeContext<ITransformAsset>> transformAssetTask;

        public DownlaodPackageConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        [SetUp]
        public void Setup()
        {
            repositoryManager.Reset();
            downloadPackageConsumer.Reset();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            downloadPackageTask = Handler<IDownloadPackage>(configurator,
                context => new DownloadPackageConsumer(repositoryManager.Object, Bus).Consume(context));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.AssetManagerTransformAssetMessageQueue, ec =>
            {
                ec.Consumer(() => downloadPackageConsumer.Object);
                transformAssetTask = Handled<ITransformAsset>(ec);
            });

            configurator.ReceiveEndpoint(BusConstants.AssetManagerAssetReadyEventQueue, ec =>
            {
                ec.Consumer(() => downloadPackageConsumer.Object);
                assetReadyUpdatedTask = Handled<IAssetReady>(ec);
            });
        }

        [Test]
        public async Task If_GetPackage_from_repository_is_successfull_transformation_of_asset_is_started()
        {
            // Arrange
            var archiveRecordId = "654";
            var packageId = "666";
            var downloadResult = new RepositoryPackageResult
            {
                Valid = true, Success = true, PackageDetails = new RepositoryPackage
                {
                    ArchiveRecordId = archiveRecordId, PackageFileName = "someZipFile.zip"
                }
            };
            repositoryManager.Setup(e => e.GetPackage(packageId, archiveRecordId, It.IsAny<int>())).ReturnsAsync(downloadResult);

            // Act
            await InputQueueSendEndpoint.Send<IDownloadPackage>(new
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await downloadPackageTask;
            var context = await transformAssetTask;

            // Assert
            context.Message.ArchiveRecordId.Should().Be(archiveRecordId);
            context.Message.CallerId = "someCaller";
            context.Message.AssetType = AssetType.Gebrauchskopie; // Download from DIR is always UsageCopy
            context.Message.FileName.Should().Be("someZipFile.zip");
            context.Message.RetentionCategory.Should().Be(CacheRetentionCategory.UsageCopyPublic);
        }

        [Test]
        public async Task If_GetPackage_from_repository_is_not_valid_AssetReady_returns_failure()
        {
            // Arrange
            var archiveRecordId = "651";
            var packageId = "646";
            var downloadResult = new RepositoryPackageResult
            {
                Valid = false,
                Success = true,
                PackageDetails = new RepositoryPackage
                {
                    ArchiveRecordId = archiveRecordId,
                    PackageFileName = "someZipFile.zip"
                }
            };
            repositoryManager.Setup(e => e.GetPackage(packageId, archiveRecordId, It.IsAny<int>())).ReturnsAsync(downloadResult);

            // Act
            await InputQueueSendEndpoint.Send<IDownloadPackage>(new
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await downloadPackageTask;
            var context = await assetReadyUpdatedTask;

            // Assert
            context.Message.ArchiveRecordId.Should().Be(archiveRecordId);
            context.Message.CallerId = "someCaller";
            context.Message.AssetType = AssetType.Gebrauchskopie; // Download from DIR is always UsageCopy
            context.Message.Valid.Should().Be(false);
        }

        [Test]
        public async Task If_GetPackage_from_repository_is_no_success_AssetReady_returns_failure()
        {
            // Arrange
            var archiveRecordId = "651";
            var packageId = "646";
            var downloadResult = new RepositoryPackageResult
            {
                Valid = false,
                Success = true,
                PackageDetails = new RepositoryPackage
                {
                    ArchiveRecordId = archiveRecordId,
                    PackageFileName = "someZipFile.zip"
                }
            };
            repositoryManager.Setup(e => e.GetPackage(packageId, archiveRecordId, It.IsAny<int>())).ReturnsAsync(downloadResult);

            // Act
            await InputQueueSendEndpoint.Send<IDownloadPackage>(new
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            await downloadPackageTask;
            var context = await assetReadyUpdatedTask;

            // Assert
            context.Message.ArchiveRecordId.Should().Be(archiveRecordId);
            context.Message.CallerId = "someCaller";
            context.Message.AssetType = AssetType.Gebrauchskopie; // Download from DIR is always UsageCopy
            context.Message.Valid.Should().Be(false);
        }
    }
}
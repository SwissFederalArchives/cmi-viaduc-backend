using System;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Repository.Consumer;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    public class DownlaodPackageConsumerTests
    {
        private readonly Mock<IConsumer<IDownloadPackage>> downloadPackageConsumer = new Mock<IConsumer<IDownloadPackage>>();
        private InMemoryTestHarness harness;
        private readonly Mock<IRepositoryManager> repositoryManager = new Mock<IRepositoryManager>();
        
        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
            harness.Start();
            repositoryManager.Reset();
            downloadPackageConsumer.Reset();
        }

        [Test]
        public async Task If_GetPackage_from_repository_is_successfull_preprocessing_of_asset_is_started()
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
            var consumer = new DownloadPackageConsumer(repositoryManager.Object, harness.Bus);
            var context = new Mock<ConsumeContext<IDownloadPackage>>();
            context.SetupGet(x => x.Message).Returns(new DownloadPackage
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });
            context.SetupGet(x => x.SourceAddress).Returns(harness.BaseAddress);
            context.Setup(c => c.GetSendEndpoint(It.IsAny<Uri>())).Returns(GetEndpoint);
           

            // Act
            await consumer.Consume(context.Object);

            // Assert  
            context.Verify(c => c.GetSendEndpoint(It.IsAny<Uri>()), Times.Once);
            context.Verify(c => c.Publish<IPackageDownloaded>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
            context.Verify(c => c.Publish<IAssetReady>(It.IsAny<AssetReady>(), It.IsAny<CancellationToken>()), Times.Never);
            await harness.Stop();
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
            var consumer = new DownloadPackageConsumer(repositoryManager.Object, harness.Bus);
            var context = new Mock<ConsumeContext<IDownloadPackage>>();
            context.SetupGet(x => x.Message).Returns(new DownloadPackage
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });
            context.Setup(c => c.GetSendEndpoint(It.IsAny<Uri>())).Returns(GetEndpoint);

            // Act
            await consumer.Consume(context.Object);
            // Assert  
            context.Verify(c => c.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            context.Verify(c => c.Publish<IPackageDownloaded>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            context.Verify(c => c.Publish<IAssetReady>(It.IsAny<AssetReady>(), It.IsAny<CancellationToken>()), Times.Once);
            await harness.Stop();
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
            var consumer = new DownloadPackageConsumer(repositoryManager.Object, harness.Bus);
            var context = new Mock<ConsumeContext<IDownloadPackage>>();
            context.SetupGet(x => x.Message).Returns(new DownloadPackage
            {
                PackageId = packageId,
                ArchiveRecordId = archiveRecordId,
                CallerId = "someCaller",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });
            context.Setup(c => c.GetSendEndpoint(It.IsAny<Uri>())).Returns(GetEndpoint);

            // Act
            await consumer.Consume(context.Object); // Assert  
            context.Verify(c => c.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            context.Verify(c => c.Publish<IPackageDownloaded>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            context.Verify(c => c.Publish<IAssetReady>(It.IsAny<AssetReady>(), It.IsAny<CancellationToken>()), Times.Once);
            await harness.Stop();
        }


        private Task<ISendEndpoint> GetEndpoint()
        {
            var endpoint = new Mock<ISendEndpoint>();
            endpoint.Setup(p =>
                    p.Send(It.IsAny<PrepareForTransformationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(PrepareForTransformationConsumerMock);
            return Task.FromResult(endpoint.Object);
        }
        
        private Task PrepareForTransformationConsumerMock()
        {
            return Task.CompletedTask;
        }
    }
}
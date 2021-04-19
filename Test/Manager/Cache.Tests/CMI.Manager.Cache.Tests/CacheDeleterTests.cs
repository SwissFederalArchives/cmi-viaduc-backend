using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Utilities.Common;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Cache.Tests
{
    [TestFixture]
    public class CacheDeleterTests
    {
        [Test]
        public async Task Deleter_Should_Delete_Two_Files_According_Schedule()
        {
            // arrange
            var mockSleeper = new Mock<ISleeper>();
            mockSleeper.Setup(m => m.Sleep(It.IsAny<TimeSpan>()));

            var parameter = new CacheSettings
            {
                RetentionSpanUsageCopyAb = "10s",
                RetentionSpanUsageCopyBarOrAS = "10s",
                RetentionSpanUsageCopyPublic = "10s",
                RetentionSpanUsageCopyEb = "0s",
                RetentionSpanUsageCopyBenutzungskopie = "20d"
            };

            var mockFile = new Mock<FileInfoBase>();
            mockFile.Setup(m => m.CreationTime).Returns(DateTime.Now);
            mockFile.Setup(m => m.LastAccessTime).Returns(DateTime.Now);

            var mockParameterHelper = new Mock<IParameterHelper>();
            mockParameterHelper
                .Setup(m => m.GetSetting<CacheSettings>())
                .Returns(parameter);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(m => m.Directory.GetFiles(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    return path.EndsWith(CacheRetentionCategory.UsageCopyEB.ToString()) ? new[] {"fileA", "fileB"} : new string[0];
                });

            mockFileSystem.Setup(m => m.FileInfo.FromFileName(It.IsAny<string>())).Returns(mockFile.Object);
            mockFileSystem.Setup(m => m.File.Delete(It.IsAny<string>()));

            var sut = new CacheDeleter(mockParameterHelper.Object, mockFileSystem.Object, mockSleeper.Object);

            // act
            await sut.Start(false);

            // assert
            mockFileSystem.Verify(m => m.File.Delete(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task Deleter_Should_Delete_One_File_According_Schedule()
        {
            // arrange
            var mockSleeper = new Mock<ISleeper>();
            mockSleeper.Setup(m => m.Sleep(It.IsAny<TimeSpan>()));

            var parameter = new CacheSettings
            {
                RetentionSpanUsageCopyAb = "10s",
                RetentionSpanUsageCopyBarOrAS = "10s",
                RetentionSpanUsageCopyPublic = "10h",
                RetentionSpanUsageCopyEb = "0s",
                RetentionSpanUsageCopyBenutzungskopie = "20d"
            };

            var mockParameterHelper = new Mock<IParameterHelper>();
            mockParameterHelper
                .Setup(m => m.GetSetting<CacheSettings>())
                .Returns(parameter);

            var mockFile = new Mock<FileInfoBase>();
            mockFile.Setup(m => m.CreationTime).Returns(DateTime.Now);
            mockFile.Setup(m => m.LastAccessTime).Returns(DateTime.Now);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(m => m.Directory.GetFiles(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    if (path.EndsWith(CacheRetentionCategory.UsageCopyEB.ToString()))
                    {
                        return new[] {"fileA"};
                    }

                    if (path.EndsWith(CacheRetentionCategory.UsageCopyPublic.ToString()))
                    {
                        return new[] {"fileB"};
                    }

                    return new string[0];
                });

            mockFileSystem.Setup(m => m.FileInfo.FromFileName(It.IsAny<string>())).Returns(mockFile.Object);
            mockFileSystem.Setup(m => m.File.Delete(It.IsAny<string>()));

            var sut = new CacheDeleter(mockParameterHelper.Object, mockFileSystem.Object, mockSleeper.Object);

            // act
            await sut.Start(false);

            // assert
            mockFileSystem.Verify(m => m.File.Delete(It.IsAny<string>()), Times.Once);
        }


        [Test]
        public async Task Deleter_Should_Delete_No_File_According_Schedule()
        {
            // arrange
            var mockSleeper = new Mock<ISleeper>();
            mockSleeper.Setup(m => m.Sleep(It.IsAny<TimeSpan>()));

            var parameter = new CacheSettings
            {
                RetentionSpanUsageCopyAb = "10s",
                RetentionSpanUsageCopyBarOrAS = "10s",
                RetentionSpanUsageCopyPublic = "10d",
                RetentionSpanUsageCopyEb = "10d",
                RetentionSpanUsageCopyBenutzungskopie = "20d"
            };

            var mockParameterHelper = new Mock<IParameterHelper>();
            mockParameterHelper
                .Setup(m => m.GetSetting<CacheSettings>())
                .Returns(parameter);

            var mockFile = new Mock<FileInfoBase>();
            mockFile.Setup(m => m.CreationTime).Returns(DateTime.Now);
            mockFile.Setup(m => m.LastAccessTime).Returns(DateTime.Now);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(m => m.Directory.GetFiles(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    if (path.EndsWith(CacheRetentionCategory.UsageCopyEB.ToString()))
                    {
                        return new[] {"fileA"};
                    }

                    if (path.EndsWith(CacheRetentionCategory.UsageCopyPublic.ToString()))
                    {
                        return new[] {"fileB"};
                    }

                    return new string[0];
                });

            mockFileSystem.Setup(m => m.FileInfo.FromFileName(It.IsAny<string>())).Returns(mockFile.Object);
            mockFileSystem.Setup(m => m.File.Delete(It.IsAny<string>()));

            var sut = new CacheDeleter(mockParameterHelper.Object, mockFileSystem.Object, mockSleeper.Object);

            // act
            await sut.Start(false);

            // assert
            mockFileSystem.Verify(m => m.File.Delete(It.IsAny<string>()), Times.Never);
        }
    }
}
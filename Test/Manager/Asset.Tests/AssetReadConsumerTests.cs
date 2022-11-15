using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Asset.Mails;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class AssetReadyConsumerTests
    {
        private Mock<IAssetManager> assetManager;
        private Mock<ICacheHelper> cacheHelper;
        private Mock<IParameterHelper> parameterHelper;
        private Mock<IMailHelper> mailHelper;
        private Mock<IDataBuilder> dataBuilder;
        private ServiceProvider provider;

        [SetUp]
        public void Setup()
        {
            assetManager = new Mock<IAssetManager>();
            cacheHelper = new Mock<ICacheHelper>();
            mailHelper = new Mock<IMailHelper>();
            // Create and configure ParameterHelper
            parameterHelper = new Mock<IParameterHelper>();
            parameterHelper.Setup(m => m.GetSetting<GebrauchskopieZumDownloadBereit>())
                .Returns(new GebrauchskopieZumDownloadBereit());
            parameterHelper.Setup(m => m.GetSetting<GebrauchskopieErstellenProblem>())
                .Returns(new GebrauchskopieErstellenProblem());
            // Create and configure DataBuilder
            dataBuilder = new Mock<IDataBuilder>();
            dataBuilder.Setup(m => m.SetDataProtectionLevel(It.IsAny<DataBuilderProtectionStatus>())).Returns(dataBuilder.Object);
            dataBuilder.Setup(m => m.AddUser(It.IsAny<string>())).Returns(dataBuilder.Object);
            dataBuilder.Setup(m => m.AddValue(It.IsAny<string>(), It.IsAny<object>())).Returns(dataBuilder.Object);
            dataBuilder.Setup(m => m.AddVe(It.IsAny<string>())).Returns(dataBuilder.Object);

            // Build the container
            provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<AssetReadyConsumer>();
                    cfg.AddTransient(_ => cacheHelper.Object);
                    cfg.AddTransient(_ => parameterHelper.Object);
                    cfg.AddTransient(_ => mailHelper.Object);
                    cfg.AddTransient(_ => dataBuilder.Object);
                    cfg.AddTransient(_ => assetManager.Object);
                    cfg.AddTransient(_ => new PasswordHelper("seed"));
                })
                .BuildServiceProvider(true);
        }

        [Test]
        public async Task Ensure_Unregister_From_Job_Queue_Called()
        {
            // Arrange
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            // Act
            await harness.Bus.Publish(new AssetReady
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                PrimaerdatenAuftragId = 3
            });

            // Assert
            Assert.IsTrue(await harness.Published.Any<AssetReady>());
            Assert.That(await harness.Consumed.Any<IAssetReady>());
            var consumerHarness = harness.GetConsumerHarness<AssetReadyConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IAssetReady>());

            assetManager.Verify(e => e.UnregisterJobFromPreparationQueue(3), Times.Once);

            await harness.Stop();
        }

        [Test]
        public async Task Ensure_Success_Mail_is_sent()
        {
            // Arrange
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            // Act
            await harness.Bus.Publish(new AssetReady
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                PrimaerdatenAuftragId = 3,
                Valid = true
            });

            // Assert
            Assert.IsTrue(await harness.Published.Any<AssetReady>());
            Assert.That(await harness.Consumed.Any<IAssetReady>());
            var consumerHarness = harness.GetConsumerHarness<AssetReadyConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IAssetReady>());

            mailHelper.Verify(e => e.SendEmail(It.IsAny<IBus>(),
                It.IsAny<GebrauchskopieZumDownloadBereit>(),
                It.IsAny<object>(),
                It.IsAny<bool>()), Times.Once);

            await harness.Stop();
        }

        [Test]
        public async Task Ensure_Failure_Mail_is_sent()
        {
            // Arrange
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            // Act
            await harness.Bus.Publish(new AssetReady
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie,
                PrimaerdatenAuftragId = 3,
                Valid = false
            });

            // Assert
            Assert.IsTrue(await harness.Published.Any<AssetReady>());
            Assert.That(await harness.Consumed.Any<IAssetReady>());
            var consumerHarness = harness.GetConsumerHarness<AssetReadyConsumer>();
            Assert.That(await consumerHarness.Consumed.Any<IAssetReady>());

            mailHelper.Verify(e => e.SendEmail(It.IsAny<IBus>(),
                It.IsAny<GebrauchskopieErstellenProblem>(),
                It.IsAny<object>(),
                It.IsAny<bool>()), Times.Once);

            await harness.Stop();
        }
    }
}
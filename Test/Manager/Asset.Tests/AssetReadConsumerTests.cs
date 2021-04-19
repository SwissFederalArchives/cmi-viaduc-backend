using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Asset.Mails;
using CMI.Utilities.Template;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class AssetReadyConsumerTests : InMemoryTestFixture
    {
        public AssetReadyConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private Task<ConsumeContext<IAssetReady>> assetReadyTask;


        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            var parameterHelper = new Mock<IParameterHelper>();
            parameterHelper.Setup(m => m.GetSetting<GebrauchskopieZumDownloadBereit>())
                .Returns(new GebrauchskopieZumDownloadBereit());
            parameterHelper.Setup(m => m.GetSetting<GebrauchskopieErstellenProblem>())
                .Returns(new GebrauchskopieErstellenProblem());

            var mailHelper = new Mock<IMailHelper>();
            mailHelper.Setup(m => m.SendEmail(It.IsAny<IBus>(), It.IsAny<EmailTemplate>(), It.IsAny<object>(), false));

            var dataBuilder = new Mock<IDataBuilder>();
            dataBuilder.Setup(m => m.AddUser(It.IsAny<string>())).Returns(dataBuilder.Object);
            dataBuilder.Setup(m => m.AddValue(It.IsAny<string>(), It.IsAny<object>())).Returns(dataBuilder.Object);
            dataBuilder.Setup(m => m.AddVe(It.IsAny<string>())).Returns(dataBuilder.Object);

            assetReadyTask = Handler<IAssetReady>(configurator,
                context => new AssetReadyConsumer(assetManager.Object, parameterHelper.Object, null, mailHelper.Object, dataBuilder.Object,
                    new PasswordHelper("seed")).Consume(context));
        }

        [Test]
        public async Task Ensure_Unregister_From_Job_Queue_Called()
        {
            // Arrange

            // Act
            await InputQueueSendEndpoint.Send<IAssetReady>(new
                {ArchiveRecordId = "999", AssetType = AssetType.Gebrauchskopie, PrimaerdatenAuftragId = 3});
            await assetReadyTask;

            // Assert
            assetManager.Verify(e => e.UnregisterJobFromPreparationQueue(3), Times.Once);
        }
    }
}
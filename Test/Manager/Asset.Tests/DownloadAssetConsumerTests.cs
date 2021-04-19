using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Cache;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class DownloadAssetConsumerTests : InMemoryTestFixture
    {
        [SetUp]
        public void Setup()
        {
            requestClient = CreateRequestClient<DownloadAssetRequest, DownloadAssetResult>();
            doesExistsClient = CreateRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>();
            assetManager.Reset();
            cacheHelper.Reset();
            downloadPackageConsumer.Reset();
            notificationManagerConsumer.Reset();
        }

        public DownloadAssetConsumerTests() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        private readonly Mock<IAssetManager> assetManager = new Mock<IAssetManager>();
        private readonly Mock<ICacheHelper> cacheHelper = new Mock<ICacheHelper>();
        private readonly Mock<IConsumer<IDownloadPackage>> downloadPackageConsumer = new Mock<IConsumer<IDownloadPackage>>();
        private readonly DoesExistInCacheRequestConsumer doesExistInCacheConsumer = new DoesExistInCacheRequestConsumer();
        private readonly Mock<IConsumer<IEmailMessage>> notificationManagerConsumer = new Mock<IConsumer<IEmailMessage>>();
        private readonly Mock<IParameterHelper> parameterHelper = new Mock<IParameterHelper>();
        private readonly Mock<IMailHelper> mailHelper = new Mock<IMailHelper>();
        private readonly Mock<IDataBuilder> dataBuilder = new Mock<IDataBuilder>();

        private Task<ConsumeContext<DownloadAssetRequest>> downloadAssetTask;
        private IRequestClient<DownloadAssetRequest, DownloadAssetResult> requestClient;
        private IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistsClient;
        private Task<DownloadAssetResult> response;
        private Task<ConsumeContext<IEmailMessage>> notificationHandled;


        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            downloadAssetTask = Handler<DownloadAssetRequest>(configurator,
                context => new DownloadAssetConsumer(
                    cacheHelper.Object,
                    Bus,
                    doesExistsClient,
                    parameterHelper.Object,
                    mailHelper.Object,
                    dataBuilder.Object,
                    new PasswordHelper("seed")
                ).Consume(context));


            Handler<DoesExistInCacheRequest>(configurator, context => doesExistInCacheConsumer.Consume(context));
        }


        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(BusConstants.NotificationManagerMessageQueue, ec =>
            {
                ec.Consumer(() => notificationManagerConsumer.Object);
                notificationHandled = Handled<IEmailMessage>(ec);
            });
        }


        [Test]
        public async Task Requested_download_not_in_cache_returns_null_for_url()
        {
            // Arrange
            doesExistInCacheConsumer.DoesExistFunc = context => new Tuple<bool, long>(false, 0);

            // Act
            response = requestClient.Request(new DownloadAssetRequest
            {
                ArchiveRecordId = "999",
                AssetType = AssetType.Gebrauchskopie
            });

            // Wait for the results
            var message = await response;
            await downloadAssetTask;

            // Assert
            message.AssetDownloadLink.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task Requested_download_returns_valid_url()
        {
            // Arrange
            doesExistInCacheConsumer.DoesExistFunc = context => new Tuple<bool, long>(true, 99999);

            cacheHelper.Setup(e => e.GetFtpUrl(Bus, CacheRetentionCategory.UsageCopyPublic, "1111")).Returns(Task.FromResult("sft://mockup"));

            // Act
            response = requestClient.Request(new DownloadAssetRequest
            {
                ArchiveRecordId = "1111",
                AssetType = AssetType.Gebrauchskopie,
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });

            // Wait for the results
            var message = await response;
            await downloadAssetTask;

            // Assert
            message.AssetDownloadLink.Should().NotBeEmpty();
        }
    }
}
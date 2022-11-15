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
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests;

[TestFixture]
public class DownloadAssetConsumerTests
{
    private Mock<IAssetManager> assetManager;
    private Mock<ICacheHelper> cacheHelper;
    private Mock<IParameterHelper> parameterHelper;
    private Mock<IMailHelper> mailHelper;
    private Mock<IDataBuilder> dataBuilder;
    private DoesExistInCacheRequestConsumer doesExistInCacheConsumer;
    private ServiceProvider provider;

    [SetUp]
    public void Setup()
    {
        assetManager = new Mock<IAssetManager>();
        cacheHelper = new Mock<ICacheHelper>();
        parameterHelper = new Mock<IParameterHelper>();
        mailHelper = new Mock<IMailHelper>();
        dataBuilder = new Mock<IDataBuilder>();
        doesExistInCacheConsumer = new DoesExistInCacheRequestConsumer();
        provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSingleton(doesExistInCacheConsumer);
                cfg.AddConsumer<DownloadAssetConsumer>();
                cfg.AddConsumer<DoesExistInCacheRequestConsumer>();
                cfg.AddTransient(_ => doesExistInCacheConsumer);
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
    public async Task Requested_download_not_in_cache_returns_null_for_url()
    {
        // Arrange
        doesExistInCacheConsumer.DoesExistFunc = _ => new Tuple<bool, long>(false, 0);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var client = harness.GetRequestClient<DownloadAssetRequest>();

        // Act
        var result = await client.GetResponse<DownloadAssetResult>(new DownloadAssetRequest
        {
            ArchiveRecordId = "999",
            AssetType = AssetType.Gebrauchskopie
        });

        // Assert
        result.Message.AssetDownloadLink.Should().BeNullOrEmpty();
        await harness.Stop();
    }

    [Test]
    public async Task Requested_download_returns_valid_url()
    {
        // Arrange
        doesExistInCacheConsumer.DoesExistFunc = _ => new Tuple<bool, long>(true, 99999);
        cacheHelper.Setup(c => c.GetFtpUrl(It.IsAny<IBus>(), It.IsAny<CacheRetentionCategory>(), "1111"))
            .Returns(() => Task.FromResult("sft://mockup"));

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var client = harness.GetRequestClient<DownloadAssetRequest>();

        // Act
        var result = await client.GetResponse<DownloadAssetResult>(new DownloadAssetRequest
        {
            ArchiveRecordId = "1111",
            AssetType = AssetType.Gebrauchskopie,
            RetentionCategory = CacheRetentionCategory.UsageCopyPublic
        });


        // Assert
        result.Message.AssetDownloadLink.Should().Be("sft://mockup");
        await harness.Stop();
    }
}
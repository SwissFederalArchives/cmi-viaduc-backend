using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Engine.Security;
using CMI.Manager.Asset.ParameterSettings;
using FluentAssertions;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class AssetManagerTests
    {
        [Test]
        public void Conversion_of_Gebrauchskopie_with_no_package_id_throws_error()
        {
            // Arrange
            var assetManager = CreateAssetManager();

            // Act
            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await assetManager.ConvertPackage("123", AssetType.Gebrauchskopie, false, "test.zip", null));
        }

        [Test]
        public async Task After_registering_job_call_to_queue_returns_true_and_after_unregister_false()
        {
            // Arrange
            var assetManager = CreateAssetManager();

            // Act and Assert 1
            await assetManager.RegisterJobInPreparationQueue("1", "1", AufbereitungsArtEnum.Download, AufbereitungsServices.AssetService,
                new List<ElasticArchiveRecordPackage>(), null);
            var result = await assetManager.CheckPreparationStatus("1");
            result.PackageIsInPreparationQueue.Should().BeTrue();

            // Act and Assert 2
            await assetManager.UnregisterJobFromPreparationQueue(2);
            result = await assetManager.CheckPreparationStatus("2");
            result.PackageIsInPreparationQueue.Should().BeFalse();


            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await assetManager.ConvertPackage("123", AssetType.Gebrauchskopie, false, "test.zip", null));
        }

        private static AssetManager CreateAssetManager()
        {
            var textEngineMock = new Mock<ITextEngine>();
            var renderEngineMock = new Mock<IRenderEngine>();
            var transformEngineMock = new Mock<ITransformEngine>();
            var passwordHelper = new PasswordHelper("just a test");
            var paramHelperMock = new Mock<IParameterHelper>();
            paramHelperMock.Setup(s => s.GetSetting<SchaetzungAufbereitungszeitSettings>()).Returns(new SchaetzungAufbereitungszeitSettings
                {KonvertierungsgeschwindigkeitVideo = 1, KonvertierungsgeschwindigkeitAudio = 1});
            paramHelperMock.Setup(s => s.GetSetting<AssetPriorisierungSettings>()).Returns(new AssetPriorisierungSettings
            {
                PackageSizes = @"
                        {
	                        ""MaxSmallSizeInMB"": 250,
	                        ""MaxMediumSizeInMB"": 1000,
	                        ""MaxLargeSizeInMB"": 4000,
	                        ""ExtraLargeSizeInMB"": 2147483647
                        }"
            });
            var scanProcessorMock = new Mock<IScanProcessor>();
            var preparationTimeCalculator = new Mock<IPreparationTimeCalculator>();
            var auftragAccess = new Mock<IPrimaerdatenAuftragAccess>();
            auftragAccess.Setup(e => e.CreateOrUpdateAuftrag(It.IsAny<PrimaerdatenAuftrag>())).Returns(Task.FromResult(1));
            auftragAccess.Setup(e => e.GetLaufendenAuftrag(1, AufbereitungsArtEnum.Download)).Returns(Task.FromResult(
                new PrimaerdatenAuftragStatusInfo
                    {PrimaerdatenAuftragId = 1, AufbereitungsArt = AufbereitungsArtEnum.Download}));
            auftragAccess.Setup(e => e.GetLaufendenAuftrag(2, AufbereitungsArtEnum.Download))
                .Returns(Task.FromResult<PrimaerdatenAuftragStatusInfo>(null));
            auftragAccess.Setup(e => e.UpdateStatus(It.IsAny<PrimaerdatenAuftragLog>(), 0)).Returns(Task.FromResult(1));
            var indexClient = new Mock<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>();
            indexClient.Setup(e => e.Request(It.IsAny<FindArchiveRecordRequest>(), CancellationToken.None)).Returns(
                Task.FromResult(new FindArchiveRecordResponse {ArchiveRecordId = "1", ElasticArchiveRecord = new ElasticArchiveRecord()}));

            preparationTimeCalculator
                .Setup(s => s.EstimatePreparationDuration(It.IsAny<List<ElasticArchiveRecordPackage>>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(TimeSpan.FromMinutes(1));
            var assetManager = new AssetManager(textEngineMock.Object, renderEngineMock.Object, transformEngineMock.Object, passwordHelper,
                paramHelperMock.Object, scanProcessorMock.Object, preparationTimeCalculator.Object, auftragAccess.Object, indexClient.Object,
                null, null);
            return assetManager;
        }
    }
}
using System.IO;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.PostProcess;
using CMI.Engine.Asset.Solr;
using Shouldly;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PostProcessIiifOcrIndexerTests
    {
        private RepositoryPackage package;
        private string sourceTempDir;
        private string destTempDir;

        [OneTimeSetUp]
        public void CreateTestData()
        {
            sourceTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            destTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            package = IIIFTestDataCreator.CreateTestData(sourceTempDir, new[] { ".hOCR" });
        }

        [OneTimeTearDown]
        public void RemoveTestData()
        {
            Directory.Delete(sourceTempDir, true);
        }


        [Test]
        public void MovingOfFiles_with_very_long_path_nameWorks()
        {
            // Arrange
            var archiveRecordId = "Vz      a2ad9673-74f1-5705-9775-7072b417d3d3";
            var sut = new PostProcessIiifOcrIndexer(
                new SolrEngine(
                new SolrConnectionInfo
                {
                    SolrUrl = "SkipSolrForTesting", SolrHighlightingPath = destTempDir
                }),
                new IiifManifestSettings())
                {
                    ArchiveRecordId = archiveRecordId,
                    RootFolder = sourceTempDir
                };
            var sourceDir = Path.Combine(TestContext.CurrentContext.TestDirectory, $@"TestData\IiifSourceData\{archiveRecordId}"); ;
           
            var metadataFile = new FileInfo(Path.Combine(sourceDir, "header", "metadata.xml"));
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile.FullName);
            sut.Paket = paket;
            // Act
            sut.AnalyzeRepositoryPackage(package, sourceTempDir);

            // Assert
            var ordner = "a2ad9673-74f\\1-5705-9775-\\7072b417d3d3";
            

            var file = Directory.GetFiles(destTempDir, "*.hOcr", SearchOption.AllDirectories);
            file[0].ShouldBe(Path.Combine(destTempDir, ordner, "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_47074F.hOCR"));
        }
    }
}

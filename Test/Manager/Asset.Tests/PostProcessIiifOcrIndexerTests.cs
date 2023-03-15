using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Engine.Asset.PostProcess;
using CMI.Engine.Asset.Solr;
using FluentAssertions;
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

            package = IIIFTestDataCreator.CreateTestData(sourceTempDir, new[] {".hOCR"});
        }

        [OneTimeTearDown]
        public void RemoveTestData()
        {
            Directory.Delete(sourceTempDir, true);
        }
      

        [Test]
        public void MovingOfFilesWorks()
        {
            // Arrange
            var sut = new PostProcessIiifOcrIndexer(new SolrConnectionInfo(){SolrUrl = "SkipSolrForTesting", SolrHighlightingPath = destTempDir});
            sut.ArchiveRecordId = "654321";
            sut.RootFolder = sourceTempDir;

            // Act
            sut.AnalyzeRepositoryPackage(package, sourceTempDir);

            // Assert
            var file = Directory.GetFiles(destTempDir, "*.hOcr", SearchOption.AllDirectories);
            file[0].Should().Be(Path.Combine(destTempDir, "0000\\0065\\4321",  "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_47074F.hOCR"));
        }
    }
}

using System.IO;
using CMI.Contract.Common;
using CMI.Engine.Asset.PostProcess;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests;

[TestFixture]
public class PostProcessIiifFileDistributorTests
{
    private RepositoryPackage package;
    private string sourceTempDir;
    private string destTempDir;

    [OneTimeSetUp]
    public void CreateTestData()
    {
        sourceTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        destTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        package = IIIFTestDataCreator.CreateTestData(sourceTempDir, new[] {".jpg", ".txt", ".pdf", "_OCR.txt", "OCR-Text-komplett.zip" });
    }

    [OneTimeTearDown]
    public void RemoveTestData()
    {
        Directory.Delete(sourceTempDir, true);
    }


    [Test]
    public void DistributionOfFilesWorks()
    {
        // Arrange
        var sut = new PostProcessIiifFileDistributor(new ViewerFileLocationSettings()
        {
            ImageOutputSaveDirectory = destTempDir,
            OcrOutputSaveDirectory = destTempDir,
            ContentOutputSaveDirectory = destTempDir,
            ManifestOutputSaveDirectory = destTempDir
        });
        sut.ArchiveRecordId = "123456";
        sut.RootFolder = sourceTempDir;

        // Act
        sut.AnalyzeRepositoryPackage(package, sourceTempDir);

        // Assert
        var txtFile = Directory.GetFiles(destTempDir, "*.txt", SearchOption.AllDirectories);
        txtFile[0].Should().Be(Path.Combine(destTempDir, "0000\\0012\\3456", "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_17D955.txt"));

        var jpgFile = Directory.GetFiles(destTempDir, "*.jpg", SearchOption.AllDirectories);
        jpgFile[0].Should().Be(Path.Combine(destTempDir, "0000\\0012\\3456", "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_47074F.jpg"));

        var pdfFile = Directory.GetFiles(destTempDir, "*.pdf", SearchOption.AllDirectories);
        pdfFile[0].Should().Be(Path.Combine(destTempDir, "0000\\0012\\3456", "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_FB7644.pdf"));

        var zipFile = Directory.GetFiles(destTempDir, "*.zip", SearchOption.AllDirectories);
        zipFile[0].Should().Be(Path.Combine(destTempDir, "0000\\0012\\3456", "This_is_a_very_long_path_name_tha_0E79E0\\This_is_another_very_long_path_na_0FC993\\Yet_another_very_long_file_name_t_06907A.zip"));

    }
}
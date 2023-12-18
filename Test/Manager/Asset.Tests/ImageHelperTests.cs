using System.IO;
using CMI.Engine.Asset.ParameterSettings;
using CMI.Engine.Asset.PreProcess;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests;

public class ImageHelperTests
{
    [Test]
    public void Reading_image_size_from_premis_file_returns_correct_dimensions()
    {
        // Arrange
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\iiifSourceData\30409374\content\Dokument_0000003\00000011.jp2");

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 300, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        var size = sut.GetImageSize(testFile);

        // Assert
        Assert.IsTrue(size.Width == 2560);
        Assert.IsTrue(size.Height == 3577);
    }

    [Test]
    public void Reading_image_size_from_premis_file__with_missing_size_info_returns_correct_dimensions()
    {
        // Arrange
        // This jp2 File does have a premis file, but without size info
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\jp2_OK\content\D_o_k_u_m_e_n_t_0000001\00000003.jp2");

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 300, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        var size = sut.GetImageSize(testFile);

        // Assert
        Assert.IsTrue(size.Width == 2551);
        Assert.IsTrue(size.Height == 3285);
    }

    [Test]
    public void Reading_image_resolution_from_premis_file_returns_correct_resolution()
    {
        // Arrange
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\iiifSourceData\30409374\content\Dokument_0000003\00000011.jp2");

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 100, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        var resolution = sut.GetResolution(testFile);

        // Assert
        Assert.IsTrue(resolution == 300);
    }

    [Test]
    public void Reading_image_resolution_from_non_existing_premis_file_returns_correct_file_or_default_resolution()
    {
        // Arrange
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\jp2_NOK\content\D_o_k_u_m_e_n_t_0000002\00000006.jp2");

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 100, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        var resolution = sut.GetResolution(testFile);

        // Assert
        Assert.IsTrue(resolution == 100);
    }

    [Test]
    public void Reading_dimensions_from_image_with_spaces_in_name_works()
    {
        // Arrange
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\jp2Sample\Datei mit Leerzeichen im Namen 00000001.jp2");

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 100, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        var size = sut.GetImageSize(testFile);

        // Assert
        Assert.IsTrue(size.Width == 2479);
    }

    [Test]
    public void Converting_image_with_spaces_in_name_works()
    {
        // Arrange
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\jp2Sample\Datei mit Leerzeichen im Namen 00000001.jp2");
        var outputFile = Path.ChangeExtension(testFile, ".jpg");
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        var sut = new ImageHelper(new ScansZusammenfassenSettings()
            { DefaultAufloesungInDpi = 100, GroesseInProzent = 100, JpegQualitaetInProzent = 100 });

        // Act
        sut.ConvertToJpeg(testFile, 100, 20);

        // Assert
        Assert.IsTrue(File.Exists(outputFile));
    }
}
using System.IO;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset;
using CMI.Engine.Asset.PreProcess;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class MetadataXmlUpdateTests
    {
        [Test]
        public void Find_file_in_xml_that_has_two_parent_folders_with_the_same_name()
        {
            // Arrange
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\metadata.xml");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var testFile = new FileInfo(
                @"D:\localdata\repository\aezg240y.nxz\content\Besuche aus dem Ausland 2008\Besuch aus Tschechien\Besuch aus Tschechien_ engültiges Programm\p000075.pdf");
            var tempFolder = "D:\\localdata\\repository\\aezg240y.nxz\\";

            // Act
            var file = MetadataXmlUpdater.GetDatei(testFile, paket, tempFolder, out var ordner);

            // Assert
            file.Should().NotBeNull();
            file.Name.Should().Be(testFile.Name);

            ordner.Should().BeOfType<OrdnerDIP>();
            ((OrdnerDIP) ordner).Id.Should().Be("COO.2080.100.2.2142784_D");
        }

        [Test]
        public void Find_file_in_root_must_return_correct_file()
        {
            // Arrange
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\metadata.xml");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var testFile = new FileInfo(@"D:\localdata\repository\aezg240y.nxz\p999999.pdf");
            var tempFolder = "D:\\localdata\\repository\\aezg240y.nxz\\";

            // Act
            var file = MetadataXmlUpdater.GetDatei(testFile, paket, tempFolder, out var ordner);

            // Assert
            file.Should().NotBeNull();
            file.Name.Should().Be(testFile.Name);

            ordner.Should().BeOfType<InhaltsverzeichnisDIP>();
        }

        [Test]
        public void Find_file_that_does_not_exist_returns_null()
        {
            // Arrange
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\metadata.xml");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var testFile = new FileInfo(
                @"D:\localdata\repository\aezg240y.nxz\content\Besuche aus dem Ausland 2008\Besuch aus Tschechien\Besuch aus Tschechien_ engültiges Programm\dummy.pdf");
            var tempFolder = "D:\\localdata\\repository\\aezg240y.nxz\\";

            // Act
            var file = MetadataXmlUpdater.GetDatei(testFile, paket, tempFolder, out var ordner);

            // Assert
            file.Should().BeNull();
            ordner.Should().BeNull();
        }
    }
}
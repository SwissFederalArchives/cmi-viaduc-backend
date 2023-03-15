using System;
using System.IO;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.PostProcess;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PostProcessManifestCreatorTests
    {
        [TearDown]
        public void TearDown()
        {
          
        }

        [SetUp]
        public void SetUp()
        {

        }


        [Test]
        [TestCase("30409374")]
        [TestCase("30409372")]
        [TestCase("6865820")]
        [TestCase("30653572")]
        public void Check_if_manifest_creation_produces_reference_output(string archiveRecordId)
        {
            // Arrange
            var tempDir = Path.Combine(TestContext.CurrentContext.TestDirectory, Path.GetRandomFileName());
            var sut = CreateManifestCreator(tempDir);

            // Act
            var sourceDir = Path.Combine(TestContext.CurrentContext.TestDirectory, $@"TestData\IiifSourceData\{archiveRecordId}"); ;
            var referenceDir = Path.Combine(TestContext.CurrentContext.TestDirectory, $@"TestData\IiifReferenceManifests\{archiveRecordId}"); ;
            var metadataFile = new FileInfo(Path.Combine(sourceDir, "header", "metadata.xml"));
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile.FullName);

            sut.CreateManifest(archiveRecordId, paket, sourceDir);

            //---------------------------------
            // Assert - Loop through all the created manifest files and check if they match the reference
            //---------------------------------
            var newManifestFiles = new DirectoryInfo(tempDir).GetFiles("*.*", SearchOption.AllDirectories);
            var refManifestFiles = new DirectoryInfo(referenceDir).GetFiles("*.*", SearchOption.AllDirectories);

            // At least the count should be the same
            refManifestFiles.Length.Should().Be(newManifestFiles.Length);

            // Check if all files are exactly the same
            foreach (var refManifestFile in refManifestFiles)
            {
                var refContent = File.ReadAllText(refManifestFile.FullName);
                var relativeFileName = refManifestFile.FullName.Substring(referenceDir.Length);
                var newFile = newManifestFiles.FirstOrDefault(f =>
                    f.FullName.Substring(tempDir.Length).Equals(relativeFileName, StringComparison.InvariantCultureIgnoreCase));
                if (newFile == null)
                {
                    throw new FileNotFoundException($"Missing manifest file {refManifestFile.FullName}");
                }

                var newContent = File.ReadAllText(newFile.FullName);

                refContent.Should().BeEquivalentTo(newContent);
            }

            // Remove temp files
            Directory.Delete(tempDir, true);
        }

        private PostProcessManifestCreator CreateManifestCreator(string manifestOutputDirectory)
        {
            var location = new ViewerFileLocationSettings
            {
                ManifestOutputSaveDirectory = manifestOutputDirectory
            };

            // Create Manifest
            var manifestCreator = new PostProcessManifestCreator(new IiifManifestSettings()
                {
                    ApiServerUri = new Uri("https://viaducdev.cmiag.ch/iiif/"),
                    ImageServerUri = new Uri("https://viaducdev.cmiag.ch/image/"),
                    PublicManifestWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/manifests/"),
                    PublicContentWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/content/"),
                    PublicOcrWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/ocr/"),
                    PublicDetailRecordUri = new Uri("https://viaducdev.cmiag.ch/clientdev/")
                },
                location);

            return manifestCreator;
        }
    }
}

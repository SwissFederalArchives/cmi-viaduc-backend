using System;
using System.IO;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.ParameterSettings;
using CMI.Engine.Asset.PreProcess;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class ScanProcessorTests
    {
        [TearDown]
        public void TearDown()
        {
            // Delete the test data directory, as PDF files were produced. These would accumulate
            var testData = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy");
            if (Directory.Exists(testData))
            {
                Directory.Delete(testData, true);
            }
        }

        [SetUp]
        public void SetUp()
        {
            var source = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData");
            var dest = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy");
            if (Directory.Exists(source))
            {
                // Now create all of the directories
                foreach (var dirPath in Directory.GetDirectories(source, "*",
                             SearchOption.AllDirectories))
                {
                    if (dirPath.Contains("jp2_OK") || dirPath.Contains("jp2_NOK"))
                    {
                        Directory.CreateDirectory(dirPath.Replace(source, dest));
                    }
                }

                // Now copy all the files
                foreach (var dirPath in Directory.GetDirectories(source, "jp2_*",
                             SearchOption.AllDirectories))
                {
                    // Copy all the files & Replaces any files with the same name
                    foreach (var newPath in Directory.GetFiles(dirPath, "*.*",
                                 SearchOption.AllDirectories))
                    {
                        
                        File.Copy(newPath, newPath.Replace(source, dest), true);
                    }
                }
            }
        }

        [Test]
        public void File_not_found_in_content_structure_results_in_exception()
        {
            // Arrange
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK\header\metadata.xml");
            var rootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            // Make the file invalid to trigger the exception by removing one file reference in the first folder.
            paket.Inhaltsverzeichnis.Ordner[0].Ordner[0].Datei.RemoveAt(0);
            var settings = new ScansZusammenfassenSettings();
            var processor = new ScanProcessor(new FileResolution(settings), settings);

            // Act(ion)
            Action action = () => processor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, rootFolder);

            // Asert
            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Files_linked_to_document_that_are_not_jp2_files_and_not_premis_result_in_unchanged_package()
        {
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK\header\metadata.xml");
            var rootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var settings = new ScansZusammenfassenSettings { GroesseInProzent = 100, DefaultAufloesungInDpi = 300, JpegQualitaetInProzent = 80 };
            var processor = new ScanProcessor(new FileResolution(settings), settings);

            // Add some weird files to the package
            AddFileToPackage("test01.txt", "D_o_k_u_m_e_n_t_0000001", paket, rootFolder);
            AddFileToPackage("test02.txt", "D_o_k_u_m_e_n_t_0000002", paket, rootFolder);
            AddFileToPackage("test03.txt", "U_m_s_c_h_l_a_g_0000001", paket, rootFolder);

            // Act
            processor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, rootFolder);

            // Assert
            // Nothing should be changed
            var contentFolder = paket.Inhaltsverzeichnis.Ordner[0];
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000001")?.Datei.Count.Should().Be(9);
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000002")?.Datei.Count.Should().Be(9);
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "U_m_s_c_h_l_a_g_0000001")?.Datei.Count.Should().Be(5);

            // Alle Dateien vorhanden?
            var dokument1 = new DirectoryInfo(Path.Combine(rootFolder, "content", "D_o_k_u_m_e_n_t_0000001"));
            var dokument2 = new DirectoryInfo(Path.Combine(rootFolder, "content", "D_o_k_u_m_e_n_t_0000002"));
            var umschlagDirectory = new DirectoryInfo(Path.Combine(rootFolder, "content", "U_m_s_c_h_l_a_g_0000001"));
            umschlagDirectory.GetFiles("*.xml").Length.Should().Be(2);
            umschlagDirectory.GetFiles("*.jp2").Length.Should().Be(2);
            dokument1.GetFiles("*.xml").Length.Should().Be(4);
            dokument1.GetFiles("*.jp2").Length.Should().Be(4);
            dokument2.GetFiles("*.xml").Length.Should().Be(4);
            dokument2.GetFiles("*.jp2").Length.Should().Be(4);
        }

        [Test]
        public void A_messed_up_metadata_file_results_in_unchanged_document_1_and_2()
        {
            // This metadata file has data that has wrongly named jp2/premis pairs, so it does not line up
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_NOK\header\metadata.xml");
            var rootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_NOK");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var settings = new ScansZusammenfassenSettings { GroesseInProzent = 100, DefaultAufloesungInDpi = 300, JpegQualitaetInProzent = 80 };
            var processor = new ScanProcessor(new FileResolution(settings), settings);

            // Act
            processor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, rootFolder);

            // Assert
            // Document 1 and 2 are the same
            // Umschlag 1 got converted to pdf
            var contentFolder = paket.Inhaltsverzeichnis.Ordner[0];
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000001")?.Datei.Count.Should().Be(8); // The original jp2 and premis
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000002")?.Datei.Count.Should().Be(8); // The original jp2 and premis
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "U_m_s_c_h_l_a_g_0000001")?.Datei.Count.Should().Be(1); // Just the pdf

            // Premis Dateien gelöscht?
            var umschlagDirectory = new DirectoryInfo(Path.Combine(rootFolder, "content", "U_m_s_c_h_l_a_g_0000001"));
            umschlagDirectory.GetFiles("*.xml").Length.Should().Be(0);
        }


        [Test]
        public void A_valid_file_is_happily_converted()
        {
            var metadataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK\header\metadata.xml");
            var rootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestDataCopy\jp2_OK");
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
            var settings = new ScansZusammenfassenSettings { GroesseInProzent = 100, DefaultAufloesungInDpi = 300, JpegQualitaetInProzent = 80 };
            var processor = new ScanProcessor(new FileResolution(settings), settings);

            // Act
            processor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, rootFolder);

            // Assert
            // Every Dokument and Umschlag got converted
            var contentFolder = paket.Inhaltsverzeichnis.Ordner[0];
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000001")?.Datei.Count.Should().Be(1); // Just the pdf
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "D_o_k_u_m_e_n_t_0000002")?.Datei.Count.Should().Be(1); // Just the pdf
            contentFolder.Ordner.FirstOrDefault(o => o.Name == "U_m_s_c_h_l_a_g_0000001")?.Datei.Count.Should().Be(1); // Just the pdf

            // Premis Dateien gelöscht?
            var dokument1 = new DirectoryInfo(Path.Combine(rootFolder, "content", "D_o_k_u_m_e_n_t_0000001"));
            var dokument2 = new DirectoryInfo(Path.Combine(rootFolder, "content", "D_o_k_u_m_e_n_t_0000002"));
            var umschlagDirectory = new DirectoryInfo(Path.Combine(rootFolder, "content", "U_m_s_c_h_l_a_g_0000001"));
            umschlagDirectory.GetFiles("*.xml").Length.Should().Be(0);
            dokument1.GetFiles("*.xml").Length.Should().Be(0);
            dokument2.GetFiles("*.xml").Length.Should().Be(0);
        }

        private void AddFileToPackage(string sampleFileName, string targetFolderInsideContent, PaketDIP paket, string rootFolder)
        {
            var newFile = Path.Combine(rootFolder, "content", targetFolderInsideContent, sampleFileName);
            using (var file = new StreamWriter(newFile))
            {
                file.Write("just a file");
            }

            MetadataXmlUpdater.AddFile(new FileInfo(newFile), new DateiParents
            {
                DossierOderDokument = paket.Ablieferung.Ordnungssystem.Ordnungssystemposition[0].Ordnungssystemposition[0].Ordnungssystemposition[0]
                    .Dossier[0].Dokument.FirstOrDefault(d => d.Titel == targetFolderInsideContent),
                OrdnerOderInhaltverzeinis = paket.Inhaltsverzeichnis.Ordner[0].Ordner.FirstOrDefault(o => o.Name == targetFolderInsideContent)
            });
        }
    }
}
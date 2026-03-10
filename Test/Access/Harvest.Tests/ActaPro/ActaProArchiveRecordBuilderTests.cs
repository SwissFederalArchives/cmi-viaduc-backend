using CMI.Access.Harvest.ActaPro;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.Tests.ActaPro
{
    [TestFixture]
    public class ActaProArchiveRecordBuilderTests
    {
        private ActaProArchiveRecordBuilder archiveRecordBuilder;

        [SetUp]
        public void Setup()
        {
            // var translationFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"sample.tab");

            var actaProClientMock = new Mock<IActaProClient>();


            var settings = new JsonSerializerSettings
            {
                DateFormatString = "dd.MM.yyyy HH:mm:ss:fff"
            };

            var dataElementFileArch = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Arch    b83a8ece-92dc-506d-9baa-4126a74c139f.json");
            var dataElementFileBest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Best    38b7baa9-59cd-5826-b9d2-be60d6395a9e.json");
            var dataElementFileBest2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Best    8a1d4ddf-e622-5b69-859f-2566e1a11da8.json");
            var dataElementFileDokum = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Dokum   9881bd41-f832-5c11-912f-d278711d59da.json");
            var dataElementFileKlas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Klas    6c63ee5b-2afa-5d42-8d0a-60cd27464ebd.json");
            var dataElementFilePET = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data", "PET 0815.json");
            var dataElementFileTBest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "TBest   c10befd9-9b9f-5ce6-aeac-76dbb1512828.json");
            var dataElementFileTBest2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "TBest   5d76f626-9df7-5d54-b08e-5289baa7dfe9.json");
            var dataElementFileTekt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Tekt    2d75a3e4-7ea8-5c35-a82e-b3a47cf31c69.json");
            var dataElementFileVor = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Vor     75f9a935-da62-4497-b93f-2c3cef47d933.json");
            var dataElementFileVz1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020.json");
            var dataElementFileVz2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Vz      685f0872-f984-4f66-88a4-e6cd873fb1d0.json");
            var dataElementFileVz3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Vz      76949ff4-e208-5858-809e-fbddb188c588.json");
            var behältnisFile1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Mg      39d4210a-d1ca-4106-a891-85875a4fd207.json");
            var behältnisFile2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Mg      0541c342-3330-58f9-b0e2-59974252a87f.json");
            var behältnisFile3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Mg      79a2585b-be3a-58dd-8dd7-4abba0476e60.json");


            var fondlisteFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Fondliste.json");
            var ancestorsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "ancestors.json");

            var documenteArch = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileArch), settings);
            var documentBest = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileBest), settings);
            var documentBest2 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileBest2), settings);
            var documentDokum = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileDokum), settings);
            var documentKlas = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileKlas), settings);
            var documentPET = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFilePET), settings);
            var documentTBest = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileTBest), settings);
            var documentTBest2 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileTBest2), settings);
            var documentTekt = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileTekt), settings);
            var documentVor = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileVor), settings);
            var documentVZ1 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileVz1), settings);
            var documentVZ2 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileVz2), settings);
            var documentVZ3 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileVz3), settings);

            var behältnisVZ1 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(behältnisFile1), settings);
            var behältnisVZ2 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(behältnisFile2), settings);
            var behältnisVZ3 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(behältnisFile3), settings);
            var fondliste = JsonConvert.DeserializeObject<SearchResultPage>(File.ReadAllText(fondlisteFile), settings);

            var ancestors = JsonConvert.DeserializeObject<DocumentAncestorsDTO>(File.ReadAllText(ancestorsFile), settings);

            actaProClientMock.Setup(c => c.GetDocumentAsync("Arch    b83a8ece-92dc-506d-9baa-4126a74c139f", "json"))
                .Returns(Task.FromResult(documenteArch));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Best    38b7baa9-59cd-5826-b9d2-be60d6395a9e", "json"))
                .Returns(Task.FromResult(documentBest));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Best    8a1d4ddf-e622-5b69-859f-2566e1a11da8", "json"))
                .Returns(Task.FromResult(documentBest2));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Dokum   9881bd41-f832-5c11-912f-d278711d59da", "json"))
                .Returns(Task.FromResult(documentDokum));
            actaProClientMock.Setup(e => e.GetDocumentAsync("Klas    6c63ee5b-2afa-5d42-8d0a-60cd27464ebd", "json"))
                .Returns(Task.FromResult(documentKlas));
            actaProClientMock.Setup(c => c.GetDocumentAsync("PET 0815", "json")).Returns(Task.FromResult(documentPET));
            actaProClientMock.Setup(c => c.GetDocumentAsync("TBest   c10befd9-9b9f-5ce6-aeac-76dbb1512828", "json"))
                .Returns(Task.FromResult(documentTBest));
            actaProClientMock.Setup(c => c.GetDocumentAsync("TBest   5d76f626-9df7-5d54-b08e-5289baa7dfe9", "json"))
                .Returns(Task.FromResult(documentTBest2));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Tekt    2d75a3e4-7ea8-5c35-a82e-b3a47cf31c69", "json"))
                .Returns(Task.FromResult(documentTekt));
            actaProClientMock.Setup(e => e.GetDocumentAsync("Vor     75f9a935-da62-4497-b93f-2c3cef47d933", "json"))
                .Returns(Task.FromResult(documentVor));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020", "json"))
                .Returns(Task.FromResult(documentVZ1));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Vz      685f0872-f984-4f66-88a4-e6cd873fb1d0", "json"))
                .Returns(Task.FromResult(documentVZ2));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Vz      76949ff4-e208-5858-809e-fbddb188c588", "json"))
                .Returns(Task.FromResult(documentVZ3));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Vz      88dfaf7c-76b6-4617-821f-093deeaff6d3", "json"))
                .Returns(Task.FromResult(documentVZ2));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Mg      39d4210a-d1ca-4106-a891-85875a4fd207", "json"))
                .Returns(Task.FromResult(behältnisVZ1));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Mg      0541c342-3330-58f9-b0e2-59974252a87f", "json"))
                .Returns(Task.FromResult(behältnisVZ2));
            actaProClientMock.Setup(c => c.GetDocumentAsync("Mg      79a2585b-be3a-58dd-8dd7-4abba0476e60", "json"))
                .Returns(Task.FromResult(behältnisVZ3));


            var docParameters = new DocumentSearchParams
            {
                DocumentTypes = ["TBest"]
            };

            docParameters.Fields = ["Bestaendeuebersicht_Bez"];

            actaProClientMock.Setup(c => c.SearchDocumentsAsync(It.IsAny<int?>(), 1000,
                It.IsAny<IEnumerable<string>>(), docParameters)).ReturnsAsync(fondliste);

           
            var content = new Dictionary<string, string> { { "id", "Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020" } };
            var result = new SearchResultPage
            {
                Content = new List<IDictionary<string, string>> { content }
            };

            actaProClientMock.Setup(c => c.SearchDocumentsAsync(It.IsAny<int?>(), 1,
                It.IsAny<IEnumerable<string>>(), It.IsAny<DocumentSearchParams>())).ReturnsAsync(result);


            actaProClientMock.Setup(e => e.GetDocumentAncestorsAsync(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(Task.FromResult(ancestors));

            actaProClientMock.Setup(c => c.GetDocumentChildrenAsync(It.IsAny<string>(), "id", string.Join(",", ActaProClientValues.AllVerzEinheitDocTypes), null, 0, 20, null))
                .Returns(Task.FromResult(new SearchResultPage() { Content = new List<IDictionary<string, string>>() }));

            var dataProvider = new ActaProAISDataProvider(actaProClientMock.Object);

            var cachedLookupData = new CachedLookupData(dataProvider);
            // Arrange
            archiveRecordBuilder = new ActaProArchiveRecordBuilder(dataProvider, cachedLookupData);
        }

        [Test]
        public void Test_VZ_Level_Is_Dossier()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020").Result;

            // Assert
            result.Metadata.DetailData.Count.Should().Be(16);
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should().Be("Dossier");
        }

        [Test]
        public void Test_Vor_Level_Is_Subdossier()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vor     75f9a935-da62-4497-b93f-2c3cef47d933").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should()
                .Be("Subdossier");
        }

        [Test]
        public void Test_Tekt_Level_Is_Hauptabteilung()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Tekt    2d75a3e4-7ea8-5c35-a82e-b3a47cf31c69").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should()
                .Be("Hauptabteilung");
        }

        [Test]
        public void Test_TBest_Level_Is_Teilbestand()
        {
            // ACT
            var result = archiveRecordBuilder.Build("TBest   c10befd9-9b9f-5ce6-aeac-76dbb1512828").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should()
                .Be("Teilbestand");
        }

        [Test]
        public void Test_PET_Level_Is_Null()
        {
            // Arrange
            // ACT
            var result = archiveRecordBuilder.Build("PET 0815").Result;

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void Test_Klas_Level_Is_Serie()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Klas    6c63ee5b-2afa-5d42-8d0a-60cd27464ebd").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should().Be("Serie");
        }

        [Test]
        public void Test_Dokum_Level_Is_Dokument()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Dokum   9881bd41-f832-5c11-912f-d278711d59da").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should().Be("Dokument");
        }

        [Test]
        public void Test_form_group_field_is_mapped()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Dokum   9881bd41-f832-5c11-912f-d278711d59da").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("FORM")).ElementValue[0].TextValues[0].Value.Should().Be("Video");
        }

        [Test]
        public void Test_Best_Level_Is_Bestand()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Best    8a1d4ddf-e622-5b69-859f-2566e1a11da8").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should().Be("Bestand");
        }

        [Test]
        public void Test_Arch_Level_Is_Archiv()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Arch    b83a8ece-92dc-506d-9baa-4126a74c139f").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value.Should().Be("Archiv");
        }


        [Test]
        public void Test_Arch_Signatur_Is_CH_BAR_Unterlagen()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Arch    b83a8ece-92dc-506d-9baa-4126a74c139f").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("CH-BAR#Unterlagen*");
        }

        [Test]
        public void Test_Best_Signatur_Is_J2_143()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Best    8a1d4ddf-e622-5b69-859f-2566e1a11da8").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("J2.143*");
        }
        [Test]
        public void Test_Best_Darin_Is_Field()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Best    38b7baa9-59cd-5826-b9d2-be60d6395a9e").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("DARIN")).ElementValue[0].TextValues[0].Value.Should()
                .Be("Die Dokumente stammen aus den Jahren 1915 bis 1919. (Quelle: BAR interne Bestandesanalyse J I.216-Az. 561-16)");
        }

        [Test]
        public void Test_Dokum_Signatur_Is_J2_143_1996_386_1119()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Dokum   9881bd41-f832-5c11-912f-d278711d59da").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("J2.143#1996/386#1119*");
        }


        [Test]
        public void Test_Klas_Signatur_Is_J2_143_20_1964()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Klas    6c63ee5b-2afa-5d42-8d0a-60cd27464ebd").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("J2.143#20.1964");
        }

        [Test]
        public void Test_TBest_Signatur_Is_E6400C()
        {
            // ACT
            var result = archiveRecordBuilder.Build("TBest   c10befd9-9b9f-5ce6-aeac-76dbb1512828").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("E6400C*");
        }

        [Test]
        public void Test_Tekt_Signatur_Is_J()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Tekt    2d75a3e4-7ea8-5c35-a82e-b3a47cf31c69").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should().Be("J*");
        }


        [Test]
        public void Test_VZ_Signatur_Is_Correct_And_No_Containers()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020").Result;

            // Assert
            result.Metadata.DetailData.Count.Should().Be(16);
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should().Be("B0#1000/1483#1085*");
            result.Metadata.Containers.Container.Count.Should().Be(0);
        }

        [Test]
        public void Test_VZ_Signatur_Is_1960_1_And_Two_Containers()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      685f0872-f984-4f66-88a4-e6cd873fb1d0").Result;

            // Assert
            result.Metadata.DetailData.Count.Should().Be(28);
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should().Be("1960/1*");
            result.Metadata.Containers.Container.Count.Should().Be(2);
            result.Metadata.Containers.Container.Any(c => c.IdName.Equals("Mg      39d4210a-d1ca-4106-a891-85875a4fd207 1234")).Should().BeTrue();
            result.Metadata.Containers.Container.Any(c => c.IdName.Equals("Mg      79a2585b-be3a-58dd-8dd7-4abba0476e60 469393")).Should().BeTrue();
        }

        [Test]
        public void Test_Vor_Signatur_Is_E1234_1000_721_8_3_And_One_Containers()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vor     75f9a935-da62-4497-b93f-2c3cef47d933").Result;

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATUR")).ElementValue[0].TextValues[0].Value.Should()
                .Be("E1234#1000/721#8#3*");
            result.Metadata.Containers.Container.Count.Should().Be(1);
        }


        /// <summary>
        /// Checks that building an archive record using a scope ID ("21687162") produces the same result
        /// as building it with the corresponding ActaPro ID ("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020").
        /// This ensures that both identifiers are treated as aliases and return identical ArchiveRecord objects.
        /// </summary>
        [Test]
        public void Test_Checks_That_Building_VE_With_ScopeId_Produces_The_Same_Result_As_Building_With_ActaProId()
        {
            // ACT
            var result1 = archiveRecordBuilder.Build("21687162").Result;
            var result2 = archiveRecordBuilder.Build("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020").Result;
            // Assert
            result1.ArchiveRecordId.Equals(result2.ArchiveRecordId).Should().BeTrue();
            result1.Metadata.DetailData.Count.Equals(result2.Metadata.DetailData.Count).Should().BeTrue();
            result1.Security.MetadataAccessToken.Count.Equals(result2.Security.MetadataAccessToken.Count).Should().BeTrue();
            result1.Security.PrimaryDataDownloadAccessToken.Count.Equals(result2.Security.PrimaryDataDownloadAccessToken.Count).Should().BeTrue();
            result1.Security.PrimaryDataFulltextAccessToken.Count.Equals(result2.Security.PrimaryDataFulltextAccessToken.Count).Should().BeTrue();

        }

        [Test]
        public void Test_ZustaendigeStelle_Is_Correct()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      010efc87-3f8c-5fb5-947f-ee9ba4693020").Result;

            // Assert
            result.Metadata.DetailData.Count.Should().Be(16);
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("ZUSTÄNDIGE_STELLE_(LINK)"))?.ElementValue[0].TextValues[0].Value.Should().Be("Schweizerisches Bundesarchiv (nach 1979)");
            result.Metadata.Containers.Container.Count.Should().Be(0);
        }

        [Test]
        public void Test_Multiple_accessions_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("TBest   5d76f626-9df7-5d54-b08e-5289baa7dfe9").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("VE_ABLIEFERUNG_LINK")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("VE_ABLIEFERUNG_LINK"));
            element?.ElementValue.Count.Should().Be(3);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("1971/185 Bundesverwaltung (1910-1950)");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("1991/111 Bundesverwaltung (1914-1951)");
            element?.ElementValue[2].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("1996/276 Bundesverwaltung (1924-1936)");
        }

        [Test]
        public void Test_Multiple_bestaendeuebersicht_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("TBest   5d76f626-9df7-5d54-b08e-5289baa7dfe9").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("VE_ORDNUNGSKOMPONENTE_LINK")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("VE_ORDNUNGSKOMPONENTE_LINK"));
            element?.ElementValue.Count.Should().Be(2);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("CH-BAR*/611 Finanzverwaltung (Gliederungseinheit)");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("CH-BAR*/612 Finanzverwaltung (Gliederungseinheit)");
        }

        [Test]
        public void Test_Multiple_countries_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("LAND")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("LAND"));
            element?.ElementValue.Count.Should().Be(3);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Schweiz");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Luxemburg");
            element?.ElementValue[2].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Deutschland");
        }

        [Test]
        public void Test_Multiple_former_reference_codes_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("SIGNATURHISTORY")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("SIGNATURHISTORY"));
            element?.ElementValue.Count.Should().Be(3);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("B0#1000/1483#2649-2654*");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("B0#1000/1483#2650*");
            element?.ElementValue[2].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("B0#1000/1483#2651*");
        }

        [Test]
        public void Test_Multiple_related_ve_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("VERWANDTE_VE")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("VERWANDTE_VE"));
            element?.ElementValue.Count.Should().Be(2);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Siehe auch B0#1000/1483#2654*");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Siehe auch B0#1000/1483#2655*");
        }

        [Test]
        public void Test_Multiple_former_aktenzeichen_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("FRÜHERES_AKTENZEICHEN")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("FRÜHERES_AKTENZEICHEN"));
            element?.ElementValue.Count.Should().Be(2);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("B0.14");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("B0.15");
        }

        [Test]
        public void Test_Multiple_digiversionen_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("DIGITALE_VERSION")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("DIGITALE_VERSION"));
            element?.ElementValue.Count.Should().Be(2);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Stream SFW_0999, Start dieses Beitrags: 03:02; Ende: 04:46");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Start dieses Beitrags: 04:46; Ende: 06:17");
            element?.ElementValue[0].Link.Value.Should().Be("Stream SFW_0999, Start dieses Beitrags: 03:02; Ende: 04:46");
            element?.ElementValue[1].Link.Value.Should().Be("Start dieses Beitrags: 04:46; Ende: 06:17");
            element?.ElementValue[0].Link.Href.Should().Be("https://media.zem.ch/01WS/1962/SFW_0999.mp4#t=182,286");
            element?.ElementValue[1].Link.Href.Should().Be("https://media.zem.ch/01WS/1962/SFW_0999.mp4#t=286,377");
        }


        [Test]
        public void Test_Multiple_zustaendige_stellen_are_read_correctly()
        {
            // ACT
            var result = archiveRecordBuilder.Build("Vz      76949ff4-e208-5858-809e-fbddb188c588").Result;

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementName.Equals("ZUSTÄNDIGE_STELLE_(LINK)")).Should().Be(1);
            var element = result.Metadata.DetailData.FirstOrDefault(d => d.ElementName.Equals("ZUSTÄNDIGE_STELLE_(LINK)"));
            element?.ElementValue.Count.Should().Be(3);
            element?.ElementValue[0].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Schweizerisches Bundesarchiv (nach 1979)");
            element?.ElementValue[1].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Bundesamt für Rüstung armasuisse (2004-)");
            element?.ElementValue[2].TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value.Should().Be("Kommando Operationen (2018-)");
            element?.ElementValue[0].EntityLink.EntityRecordId.Should().Be("Part    5225f107-a42f-5ed3-98d1-71e74ae1b0da");
            element?.ElementValue[1].EntityLink.EntityRecordId.Should().Be("Part    200a5421-e3fc-54f3-b191-5c41ffd9b5e7");
            element?.ElementValue[2].EntityLink.EntityRecordId.Should().Be("Part    2cedbfb1-a2a6-56bf-8cc6-2c0c66930a80");
        }
    }
}
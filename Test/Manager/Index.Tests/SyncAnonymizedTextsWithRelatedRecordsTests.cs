using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Engine.Anonymization;
using CMI.Manager.Index.Config;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests;

[TestFixture]
public class SyncAnonymizedTextsWithRelatedRecordsTests
{
    [Test]
    public void If_archive_record_id_is_not_numeric_nothing_is_made()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine, null);
        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "test",
        };

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);

        // Assert
        anonymizationWithManuelleKorrekturEngine.Verify(f => f.UpdateDependentRecords(It.IsAny<ElasticArchiveDbRecord>()), Times.Never);
        dbManuelleKorrekturAccessMock.Verify(f => f.GetManuelleKorrektur(It.IsAny<int>()), Times.Never);
        result.ArchiveRecordId.Should().Be(elasticRecord.ArchiveRecordId);
    }

    [Test]
    public void If_No_ManuelleKorrektur_exists_then_no_updates_are_made()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine, null);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            ParentArchiveRecordId = "12",
            FieldAccessTokens = new List<string> { "BAR" },
            IsAnonymized = false,
            Title = "Ball ███",
            UnanonymizedFields = new UnanonymizedFields
            {
                Title = "Ball Geheim"
            }
        };

        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult((ManuelleKorrekturDto) null));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);

        // Assert
        anonymizationWithManuelleKorrekturEngine.Verify(f => f.UpdateDependentRecords(It.IsAny<ElasticArchiveDbRecord>()), Times.Once);
        result.Should().Be(elasticRecord);
    }

    [Test]
    public void If_ManuelleKorrektur_exists_and_original_title_value_not_changed_then_use_title_from_manuelle_korrektur()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        ManuelleKorrekturDto syncManuelleKorrekturResult = null;
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine,
            (x) => syncManuelleKorrekturResult = x);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            ParentArchiveRecordId = "12",
            FieldAccessTokens = new List<string> { "BAR" },
            Title = "Heinz Harald Geschichten███",
            UnanonymizedFields = new UnanonymizedFields
            {
                Title = "Heinz Harald Geschichtenbuch"
            }
        };

        var manuelleKorrektur = new ManuelleKorrekturDto
        {
            VeId = 7,
            Aktenzeichen = "XY",
            AnonymisiertZumErfassungszeitpunk = true,
            Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.Published,
            Entstehungszeitraum = "1982-2090",
            ManuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
            {
                new()
                {
                    Automatisch = "Heinz Harald Geschichten███",
                    Feldname = ManuelleKorrekturFelder.Titel,
                    Manuell = "Heinz Harald ███",
                    Original = "Heinz Harald Geschichtenbuch"
                }
            }
        };

        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult(manuelleKorrektur));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);
        
        // Assert
        result.Title.Should().Be(manuelleKorrektur.ManuelleKorrekturFelder[0].Manuell, "Das manuelle Feld muss verwendet worden sein");
        syncManuelleKorrekturResult.Should().BeNull("Update Manuelle Korrektur was not called --> result is null");
        anonymizationWithManuelleKorrekturEngine.Verify(f => f.UpdateDependentRecords(It.IsAny<ElasticArchiveDbRecord>()), Times.Once);
    }

    [Test]
    public void If_ManuelleKorrektur_exists_and_original_title_value_was_changed_then_mark_manuelle_koorektur_for_revision()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        ManuelleKorrekturDto syncManuelleKorrekturResult = null;
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine,
            (x) => syncManuelleKorrekturResult = x);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            FieldAccessTokens = new List<string> { "BAR" },
            IsAnonymized = false,
            Title = "Heinz Harald Geschichten███",
            UnanonymizedFields = new UnanonymizedFields
            {
                Title = "Heinz Harald Geschichten Buch"  // This is different than Geschichtenbuch
            }
        };

        var manuelleKorrektur = new ManuelleKorrekturDto
        {
            VeId = 7,
            Aktenzeichen = "XY",
            AnonymisiertZumErfassungszeitpunk = true,
            Anonymisierungsstatus = 1,
            Entstehungszeitraum = "1982-2090",
            ManuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
            {
                new()
                {
                    Automatisch = "Heinz Harald Geschichten███",
                    Feldname = ManuelleKorrekturFelder.Titel,
                    Manuell = "Heinz Harald ███",
                    Original = "Heinz Harald Geschichtenbuch"
                }
            }
        };
        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult(manuelleKorrektur));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);
        
        // Assert
        result.Title.Should().Be(elasticRecord.Title, "Title was not updated");
        syncManuelleKorrekturResult.ManuelleKorrekturFelder.FirstOrDefault(
                feld => feld.Feldname == ManuelleKorrekturFelder.Titel)
            ?.Manuell.StartsWith("=== Überprüfung erforderlich ===").Should().BeTrue("Hinweis muss gesetzt sein");
        syncManuelleKorrekturResult.ManuelleKorrekturFelder.FirstOrDefault(
                feld => feld.Feldname == ManuelleKorrekturFelder.Titel)
            ?.Original.Equals(elasticRecord.UnanonymizedFields.Title).Should().BeTrue("Der gelieferte Titel muss neu vorhanden sein.");
        syncManuelleKorrekturResult.Anonymisierungsstatus.Should().Be((int) AnonymisierungsStatusEnum.CheckRequired);
        anonymizationWithManuelleKorrekturEngine.Verify(f => f.UpdateDependentRecords(It.IsAny<ElasticArchiveDbRecord>()), Times.Never);
    }

    [Test]
    public void If_ManuelleKorrektur_exists_but_is_not_published_then_update_the_fields_in_the_manuelle_korrektur_but_verify_that_it_is_not_used()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        ManuelleKorrekturDto syncManuelleKorrekturResult = null;
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine,
            (x) => syncManuelleKorrekturResult = x);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            ParentArchiveRecordId = "12",
            FieldAccessTokens = new List<string> { "BAR" },
            IsAnonymized = false,
            Title = "Heinz Harald Geschichten ███",
            UnanonymizedFields = new UnanonymizedFields
            {
                Title = "Heinz Harald Geschichten Buch"
            }
        };
        var manuelleKorrektur = new ManuelleKorrekturDto
        {
            VeId = 7,
            Aktenzeichen = "XY",
            AnonymisiertZumErfassungszeitpunk = true,
            Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.InProgress,
            Entstehungszeitraum = "1982-2090",
            ManuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
            {
                new()
                {
                    Automatisch = "Heinz Harald Geschichten███",
                    Feldname = ManuelleKorrekturFelder.Titel,
                    Manuell = "Heinz Harald ███",
                    Original = "Heinz Harald Geschichtenbuch"
                }
            }
        };
        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult(manuelleKorrektur));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);

        // Assert
        result.Title.Should().Be(elasticRecord.Title, "Title was not updated");
        syncManuelleKorrekturResult.ManuelleKorrekturFelder.FirstOrDefault(feld => feld.Feldname == ManuelleKorrekturFelder.Titel)
            ?.Manuell.StartsWith("=== Überprüfung erforderlich ===").Should().BeTrue("Hinweis muss gesetzt sein");
        syncManuelleKorrekturResult.ManuelleKorrekturFelder.FirstOrDefault(feld => feld.Feldname == ManuelleKorrekturFelder.Titel)
            ?.Original.Equals(elasticRecord.UnanonymizedFields.Title).Should().BeTrue("Der gelieferte Titel muss neu vorhanden sein.");
        syncManuelleKorrekturResult.ManuelleKorrekturFelder.FirstOrDefault(feld => feld.Feldname == ManuelleKorrekturFelder.Titel)
            ?.Automatisch.Equals(elasticRecord.Title).Should().BeTrue("Der gelieferte anonymisierte Titel muss neu vorhanden sein.");
        syncManuelleKorrekturResult.Anonymisierungsstatus.Should().Be((int) AnonymisierungsStatusEnum.CheckRequired);
        anonymizationWithManuelleKorrekturEngine.Verify(f => f.UpdateDependentRecords(It.IsAny<ElasticArchiveDbRecord>()), Times.Never);
    }


    [Test] 
    public void If_record_indicates_that_it_is_not_anonymized_but_manuelle_korrektur_exits_that_is_anonymized_then_result_must_be_anonymized()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        ManuelleKorrekturDto syncManuelleKorrekturResult = null;
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine,
            (x) => syncManuelleKorrekturResult = x);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            MetadataAccessTokens = new List<string> { "BAR" },
            PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
            PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
            FieldAccessTokens = new List<string> { "BAR" },
            IsAnonymized = false,
            ParentArchiveRecordId = "12",
            Title = "Irgendein Titel",
            CustomFields =  new ExpandoObject(),
            UnanonymizedFields = new UnanonymizedFields
            {
                BemerkungZurVe = "Heinz Harald Geschichtenbuch"
            }
        };
        elasticRecord.SetCustomProperty("bemerkungZurVe", "Heinz Harald Geschichtenbuch");
        var manuelleKorrektur = new ManuelleKorrekturDto
        {
            VeId = 7,
            Aktenzeichen = "XY",
            AnonymisiertZumErfassungszeitpunk = true,
            Anonymisierungsstatus = 1,
            ManuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
            {
                new()
                {
                    Automatisch = "Heinz Harald Geschichtenbuch",
                    Feldname = ManuelleKorrekturFelder.BemerkungZurVe,
                    Manuell = "Heinz Harald ███",
                    Original = "Heinz Harald Geschichtenbuch"
                }
            }
        };
        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult(manuelleKorrektur));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);

        // Assert
        result.ZusätzlicheInformationen().Should().Be(manuelleKorrektur.ManuelleKorrekturFelder.First().Manuell);
        syncManuelleKorrekturResult.Should().BeNull("Manuelle Korrektur was not changed, thus insert/update was not called");
        result.IsAnonymized.Should().BeTrue();
    }

    [Test]
    public void If_record_indicates_that_it_is_anonymized_but_manuelle_korrektur_exits_that_is_not_anonymized_then_result_must_be_not_anonymized()
    {
        // Arrange
        Mock<ISearchIndexDataAccess> dbAccessMock = new();
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock = new();
        Mock<IAnonymizationEngine> anomizationEngineMock = new();
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine = new();
        ManuelleKorrekturDto syncManuelleKorrekturResult = null;
        var indexManager = SetupIndexManager(dbAccessMock, anomizationEngineMock, dbManuelleKorrekturAccessMock, anonymizationWithManuelleKorrekturEngine,
            (x) => syncManuelleKorrekturResult = x);

        var elasticRecord = new ElasticArchiveDbRecord
        {
            ArchiveRecordId = "7",
            MetadataAccessTokens = new List<string> { "BAR" },
            PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
            PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
            FieldAccessTokens = new List<string> { "BAR" },
            IsAnonymized = true,
            ParentArchiveRecordId = "12",
            WithinInfo = "Heinz Harald Geschichten███",
            UnanonymizedFields = new UnanonymizedFields
            {
                WithinInfo = "Heinz Harald Geschichtenbuch"
            }
        };
        var manuelleKorrektur = new ManuelleKorrekturDto
        {
            VeId = 7,
            Aktenzeichen = "XY",
            AnonymisiertZumErfassungszeitpunk = true,
            Anonymisierungsstatus = 1,
            ManuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
            {
                new()
                {
                    Automatisch = "Heinz Harald Geschichten███",
                    Feldname = ManuelleKorrekturFelder.Darin,
                    Manuell = "Heinz Harald Geschichtenbuch",
                    Original = "Heinz Harald Geschichtenbuch"
                }
            }
        };
        dbManuelleKorrekturAccessMock.Setup(db => db.GetManuelleKorrektur(It.IsAny<Func<ManuelleKorrektur, bool>>())).Returns(
            Task.FromResult(manuelleKorrektur));

        // Act
        var result = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticRecord);

        // Assert
        result.WithinInfo.Should().Be(manuelleKorrektur.ManuelleKorrekturFelder.First().Manuell);
        syncManuelleKorrekturResult.Should().BeNull("Manuelle Korrektur was not changed, thus insert/update was not called");
        result.IsAnonymized.Should().BeFalse();
    }

    private IIndexManager SetupIndexManager(Mock<ISearchIndexDataAccess> dbAccessMock,
        Mock<IAnonymizationEngine> anomizationEngineMock,
        Mock<IManuelleKorrekturAccess> dbManuelleKorrekturAccessMock,
        Mock<IAnonymizationReferenceEngine> anonymizationWithManuelleKorrekturEngine, Func<ManuelleKorrekturDto, ManuelleKorrekturDto> partialResultFunction)
    {
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
        var config = new CustomFieldsConfiguration(configFile);

        var indexManager = new IndexManager(dbAccessMock.Object, config, anomizationEngineMock.Object, dbManuelleKorrekturAccessMock.Object, anonymizationWithManuelleKorrekturEngine.Object);
        dbManuelleKorrekturAccessMock.Setup(db => db.InsertOrUpdateManuelleKorrektur(It.IsAny<ManuelleKorrekturDto>(), It.IsAny<string>()))
            .Returns<ManuelleKorrekturDto, string>((x, _) => Task.FromResult(partialResultFunction.Invoke(x)));

        dbAccessMock.Setup(db =>
            db.FindDbDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns
            <string, bool>(GetElasticArchiveDbRecordMoq);

        return indexManager;
    }


    private ElasticArchiveDbRecord GetElasticArchiveDbRecordMoq(string archiveRecordIdOrSignature, bool includeFulltextContent)
    {
        if (archiveRecordIdOrSignature == "12")
        {
            return new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "12",
                ParentArchiveRecordId = "80012345",
                IsAnonymized = true,
                Title = "Ball ███",
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = "Ball Geheim"
                },
                ArchiveplanContext = new List<ElasticArchiveplanContextItem>
                {
                    new()
                    {
                        ArchiveRecordId = "8051",
                        Protected = true,
                        Title = "Einsteins Ideen"
                    },
                    new()
                    {
                        ArchiveRecordId = "7",
                        Protected = true,
                        Title = "Korregiere mich"
                    }
                }
            };
        }

        if (archiveRecordIdOrSignature == "21")
        {
            return new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "21",
                ParentArchiveRecordId = "12",
                IsAnonymized = true,
                Title = "Einsteins Ideen",
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = "Ball Geheim"
                },
                ArchiveplanContext = new List<ElasticArchiveplanContextItem>
                {
                    new()
                    {
                        ArchiveRecordId = "8051",
                        Protected = true,
                        Title = "Test Titel"
                    },
                    new()
                    {
                        ArchiveRecordId = "7",
                        Protected = true,
                        Title = "Korregiere mich"
                    },
                    new()
                    {
                        ArchiveRecordId = "5",
                        Protected = true,
                        Title = "Korregiere mich 84"
                    }
                }
            };
        }

        return null;
    }

}
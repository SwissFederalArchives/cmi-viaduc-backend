using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.PackageMetadata;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    [TestFixture]
    public class PackageHandlerTests
    {
        [Test]
        public void Finding_nested_ordnungssystemposition_returns_correct_item()
        {
            // Arrange
            var sut = new PackageHandler(null, null, null);
            var dip = (PaketDIP) Paket.LoadFromFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "dipTestdata1.xml"));

            // Act
            var value = sut.FindOrdnungssystemPositionInPackage(new FolderInfo {Id = "I1509"}, dip);

            // Assert
            value.Id.Should().Be("I1509");
        }

        [Test]
        public void Finding_nested_dossier_returns_correct_item()
        {
            // Arrange
            var sut = new PackageHandler(null, null, null);
            var dip = (PaketDIP) Paket.LoadFromFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "dipTestdata1.xml"));

            // Act
            var value = sut.FindDossierInPackage(new FolderInfo {Id = "EkYg"}, dip);

            // Assert
            value.Id.Should().Be("EkYg");
        }

        [Test]
        // Test is more to check the beviour of the expando property in the ElasticArchiveRecordClass
        public void ArchiveRecord_is_null_is_handled_gracefully()
        {
            // Arrange
            var sut = GetNullArchiveRecord();

            // Act
            var value = sut?.Aktenzeichen();
            var value2 = sut?.CreationPeriod;

            // Assert
            value.Should().BeNullOrEmpty();
            value2.Should().BeNull();
        }

        [Test]
        public void ArchiveRecord_is_valid_returns_correct_values()
        {
            // Arrange
            var sut = GetSampleArchiveRecord();

            // Act
            var value = sut.Aktenzeichen();
            var value2 = sut.ZusätzlicheInformationen();
            var value3 = sut.CreationPeriod;

            // Assert
            value.Should().Be("Ein Beispiel");
            value2.Should().BeNullOrEmpty();
            value3.StartDate.Should().Be(DateTime.Today);
        }

        [Test]
        public void Enum_values_are_correctly_serialized()
        {
            // Arrange
            var sut = new PaketDIP();

            // Act
            sut.Generierungsdatum = DateTime.Today;
            sut.SchemaVersion = SchemaVersion.Item41;

            sut.SchemaLocation = "http://bar.admin.ch/gebrauchskopie/v1 gebrauchskopie.xsd";
            var serialized = sut.Serialize();

            // Assert
            serialized.Should().Contain("schemaVersion=\"4.1\"");
            serialized.Should().Contain("schemaLocation=\"http://bar.admin.ch/gebrauchskopie/v1 gebrauchskopie.xsd\"");
        }

        [Test]
        public void Zusatzdaten_are_correctly_serialized()
        {
            // Arrange
            var sut = new PaketDIP();

            // Act
            sut.Generierungsdatum = DateTime.Today;
            sut.SchemaVersion = SchemaVersion.Item41;

            sut.Ablieferung = new AblieferungDIP
            {
                AblieferndeStelle = "ablieferndeStelle",
                Ordnungssystem = new OrdnungssystemDIP
                {
                    Ordnungssystemposition = new List<OrdnungssystempositionDIP>
                    {
                        new OrdnungssystempositionDIP
                        {
                            Dossier = new List<DossierDIP>
                            {
                                new DossierDIP
                                {
                                    Id = "myDossierId",
                                    zusatzDaten = new List<ZusatzDatenMerkmal>
                                    {
                                        new ZusatzDatenMerkmal {Name = "propName1", Value = "Value1"},
                                        new ZusatzDatenMerkmal {Name = "propName2", Value = "Value2"}
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var serialized = sut.Serialize();

            // Assert
            serialized.Should().Contain("<zusatzDaten>");
            serialized.Should().Contain("<merkmal name=\"");
        }

        [Test]
        public void Empty_Zusatzdaten_are_not_serialized()
        {
            // Arrange
            var sut = new PaketDIP();

            // Act
            sut.Generierungsdatum = DateTime.Today;
            sut.SchemaVersion = SchemaVersion.Item41;

            sut.Ablieferung = new AblieferungDIP
            {
                AblieferndeStelle = "ablieferndeStelle",
                Ordnungssystem = new OrdnungssystemDIP
                {
                    Ordnungssystemposition = new List<OrdnungssystempositionDIP>
                    {
                        new OrdnungssystempositionDIP
                        {
                            Dossier = new List<DossierDIP>
                            {
                                new DossierDIP
                                {
                                    Id = "myDossierId"
                                }
                            }
                        }
                    }
                }
            };

            var serialized = sut.Serialize();

            // Assert
            serialized.Should().NotContain("<zusatzDaten");
        }

        private ElasticArchiveRecord GetNullArchiveRecord()
        {
            return null;
        }

        private ElasticArchiveRecord GetSampleArchiveRecord()
        {
            var record = new ElasticArchiveRecord
            {
                CreationPeriod = new ElasticTimePeriod {StartDate = DateTime.Today},
                CustomFields = new ExpandoObject()
            };
            record.CustomFields.aktenzeichen = "Ein Beispiel";
            return record;
        }
    }
}
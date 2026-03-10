using System;
using System.Collections.Generic;
using CMI.Access.Harvest.ActaPro;
using CMI.Access.Harvest.ActaPro.Security;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Access.Harvest.Tests.ActaPro
{
    [TestFixture]
    public class AccessTokenProviderTests
    {
        [Test]
        public void Metadata_AT_is_empty_if_status_in_bearbeitung()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusInBearbeitung,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                MetadatenPublizierbar = true
            };

            // Act
            var result = provider.GetMetadataAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        public void Metadata_AT_is_empty_if_zugaenglichkeit_verboten()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitVerboten,
                MetadatenPublizierbar = true
            };

            // Act
            var result = provider.GetMetadataAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        public void Metadata_AT_is_BAR_to_OE3_if_metadaten_publizierbar_is_false()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                MetadatenPublizierbar = false
            };

            // Act
            var result = provider.GetMetadataAccessTokens(data);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS");
            result.Should().Contain("BVW");
            result.Should().Contain("Ö3");
            result.Should().NotContain("Ö2");
            result.Should().NotContain("Ö1");
        }

        [Test]
        public void Metadata_AT_is_BAR_to_OE1_if_metadaten_publizierbar_is_true()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                MetadatenPublizierbar = true
            };

            // Act
            var result = provider.GetMetadataAccessTokens(data);

            // Assert
            result.Count.Should().Be(6);
            result.Should().Contain("BAR");
            result.Should().Contain("AS");
            result.Should().Contain("BVW");
            result.Should().Contain("Ö3");
            result.Should().Contain("Ö2");
            result.Should().Contain("Ö1");
        }

        [Test]
        public void Field_AT_is_empty_if_status_in_bearbeitung()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusInBearbeitung,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitGesperrt,
                Stufe = ActaProClientValues.StufeDossier,
                ZustaendigeStellenKeys = ["AS_1", "AS_2", "AS_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        public void Field_AT_is_empty_if_zugaenglichkeit_verboten()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitVerboten,
                Stufe = ActaProClientValues.StufeDossier,
                ZustaendigeStellenKeys = ["AS_1", "AS_2", "AS_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        [TestCase(ActaProClientValues.StufeArchiv)]
        [TestCase(ActaProClientValues.StufeBestand)]
        [TestCase(ActaProClientValues.StufeTeilbestand)]
        [TestCase(ActaProClientValues.StufeHauptabteilung)]
        [TestCase(ActaProClientValues.StufeSerie)]
        public void Field_AT_is_empty_if_Stufe_is_not_document_or_dossier_level(string stufe)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitGesperrt,
                Stufe = stufe,
                ZustaendigeStellenKeys = ["AS_1", "AS_2", "AS_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
        }

        [Test]
        [TestCase(null)]
        [TestCase("2099-12-31")]
        public void Field_AT_is_not_empty_if_gesperrt_and_in_schutzfrist(DateTime? schutzfristEnde)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitGesperrt,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = schutzfristEnde, 
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
            result.Should().Contain("AS_Key_3");
        }

        [Test]
        [TestCase("2000-12-31")]
        [TestCase("1970-01-01")]
        public void Field_AT_is_not_empty_if_gesperrt_and_not_in_schutzfrist(DateTime? schutzfristEnde)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitGesperrt,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = schutzfristEnde,
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
            result.Should().NotContain("BAR");
            result.Should().NotContain("AS_Key_1");
            result.Should().NotContain("AS_Key_2");
            result.Should().NotContain("AS_Key_3");
        }

        [Test]
        [TestCase(null)]
        [TestCase("2099-12-31")]
        public void Field_AT_is_not_empty_if_not_gesperrt_and_in_schutzfrist(DateTime? schutzfristEnde)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = schutzfristEnde,
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_3"]
            };

            // Act
            var result = provider.GetFieldAccessTokens(data);

            // Assert
            result.Count.Should().Be(0);
            result.Should().NotContain("BAR");
            result.Should().NotContain("AS_Key_1");
            result.Should().NotContain("AS_Key_2");
            result.Should().NotContain("AS_Key_3");
        }

        [Test]
        [TestCase(null)]
        [TestCase("2099-12-31")]
        public void Download_AT_is_BAR_and_AS_Keys_if_in_schutzfrist(DateTime? schutzfristEnde)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = schutzfristEnde,
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_3"],
                Publikationsrechte = ActaProClientValues.PublikationsrechteBAR
            };

            // Act
            var result = provider.GetDownloadAccessTokens(data);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
            result.Should().Contain("AS_Key_3");
        }

        [Test]
        [TestCase(ActaProClientValues.PublikationsrechteDritte)]
        [TestCase(ActaProClientValues.PublikationsrechtePruefungNoetig)]
        [TestCase(ActaProClientValues.PublikationsrechteUnbekannt)]
        public void Download_AT_is_BAR_and_AS_Keys_if_not_in_schutzfrist_with_publikationsrechte_restricted(string publikationsrechte)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Now.AddYears(-10),
                ZustaendigeStellenKeys = ["Key_1", "Key_2"],
                Publikationsrechte = publikationsrechte
            };

            // Act
            var result = provider.GetDownloadAccessTokens(data);

            // Assert
            result.Count.Should().Be(3);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
        }

        [Test]
        public void Download_AT_is_almost_public_if_not_in_schutzfrist_and_frei_zugaenglich()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Now.AddYears(-10),
                ZustaendigeStellenKeys = ["Key_1", "Key_2"],
                Publikationsrechte = ActaProClientValues.PublikationsrechteBAR
            };

            // Act
            var result = provider.GetDownloadAccessTokens(data);

            // Assert
            result.Count.Should().Be(5);
            result.Should().Contain("BAR");
            result.Should().Contain("AS");
            result.Should().Contain("BVW");
            result.Should().Contain("Ö3");
            result.Should().Contain("Ö2");
        }

        [Test]
        public void Download_AT_is_restricted_if_zugaenglichkeit_bga_is_pruefung_noetig()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Zugaenglichkeit = ActaProClientValues.ZugaenglichkeitNichtOeffentlich,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAPruefungNoetig,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Now.AddYears(-10),
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_99"],
                Publikationsrechte = ActaProClientValues.PublikationsrechteBAR
            };

            // Act
            var result = provider.GetDownloadAccessTokens(data);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
            result.Should().Contain("AS_Key_99");
        }

        [Test]
        public void Fulltext_AT_is_restricted_if_schutzfristende_in_zukunft()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Now.AddYears(10),
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_99"],
            };
            var ancestors = new List<SecurityCalculationInputData>();

            // Act
            var result = provider.GetFulltextAccessTokens(data, ancestors);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
            result.Should().Contain("AS_Key_99");
        }

        [Test]
        public void Fulltext_AT_is_restricted_if_schutzfristkategorie_and_enstehungszeitraum_younger_50_years()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Today.AddYears(-1),
                EntstehungszeitraumBis = DateTime.Today.AddYears(-5),
                Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt91,
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_99"],
            };
            var ancestors = new List<SecurityCalculationInputData>();

            // Act
            var result = provider.GetFulltextAccessTokens(data, ancestors);

            // Assert
            result.Count.Should().Be(4);
            result.Should().Contain("BAR");
            result.Should().Contain("AS_Key_1");
            result.Should().Contain("AS_Key_2");
            result.Should().Contain("AS_Key_99");
        }

        [Test]
        public void Fulltext_AT_is_available_if_schutzfristkategorie_9_2_and_enstehungszeitraum_older_50_years()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = ActaProClientValues.StufeDossier,
                SchutzfristEnde = DateTime.Today.AddYears(-1),
                EntstehungszeitraumBis = DateTime.Today.AddYears(-55),
                Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt92,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                ZustaendigeStellenKeys = ["Key_1", "Key_2", "Key_99"],
            };
            var ancestors = new List<SecurityCalculationInputData>();

            // Act
            var result = provider.GetFulltextAccessTokens(data, ancestors);

            // Assert
            result.Count.Should().Be(5);
            result.Should().Contain("BAR");
            result.Should().Contain("AS");
            result.Should().Contain("BVW");
            result.Should().Contain("Ö3");
            result.Should().Contain("Ö2");
        }

        [Test]
        public void Fulltext_AT_is_not_available_if_AT_is_calculated_from_protected_parent()
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = ActaProClientValues.StufeDokument,
                SchutzfristEnde = DateTime.Today.AddYears(-1),
                EntstehungszeitraumBis = DateTime.Today.AddYears(-55),
                Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt92,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                ZustaendigeStellenKeys = ["Key_1", "Key_2"],
            };
            var ancestors = new List<SecurityCalculationInputData>()
            {
                new() {SchutzfristEnde = DateTime.Today.AddYears(-25), Stufe = ActaProClientValues.StufeSerie},
                new()
                {
                    SchutzfristEnde = DateTime.Today.AddYears(25),
                    Stufe = ActaProClientValues.StufeDossier,
                    ZustaendigeStellenKeys = new List<string>()
                }
            };

            // Act
            var result = provider.GetFulltextAccessTokens(data, ancestors);

            // Assert
            result.Count.Should().Be(1);
            result.Should().Contain("BAR");
        }

        [Test]
        [TestCase(ActaProClientValues.StufeArchiv, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeHauptabteilung, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeBestand, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeTeilbestand, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeSerie, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeDossier, ExpectedResult = 5)]
        [TestCase(ActaProClientValues.StufeSubdossier, ExpectedResult = 5)]
        [TestCase(ActaProClientValues.StufeDokument, ExpectedResult = 5)]
        public int Fulltext_AT_is_empty_on_levels_above_dossier(string stufe)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = stufe,
                SchutzfristEnde = DateTime.Today.AddYears(-1),
                EntstehungszeitraumBis = DateTime.Today.AddYears(-55),
                Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt92,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                ZustaendigeStellenKeys = ["Key_1", "Key_2"],
            };
            var ancestors = new List<SecurityCalculationInputData>()
            {
                new() {SchutzfristEnde = DateTime.Today.AddYears(-25), Stufe = ActaProClientValues.StufeSerie},
                new()
                {
                    Stufe = ActaProClientValues.StufeDossier,
                    SchutzfristEnde = DateTime.Today.AddYears(-1),
                    EntstehungszeitraumBis = DateTime.Today.AddYears(-55),
                    Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt92,
                    ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                    ZustaendigeStellenKeys = new List<string>()
                }
            };

            // Act
            var result = provider.GetFulltextAccessTokens(data, ancestors);

            // Assert
            return result.Count;
        }

        [Test]
        [TestCase(ActaProClientValues.StufeArchiv, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeHauptabteilung, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeBestand, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeTeilbestand, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeSerie, ExpectedResult = 0)]
        [TestCase(ActaProClientValues.StufeDossier, ExpectedResult = 5)]
        [TestCase(ActaProClientValues.StufeSubdossier, ExpectedResult = 5)]
        [TestCase(ActaProClientValues.StufeDokument, ExpectedResult = 5)]
        public int Download_AT_is_empty_on_levels_above_dossier(string stufe)
        {
            // Arrange
            var provider = new AccessTokenProvider();
            var data = new SecurityCalculationInputData()
            {
                BearbeitungsStatus = ActaProClientValues.StatusAbgeschlossen,
                Stufe = stufe,
                SchutzfristEnde = DateTime.Today.AddYears(-1),
                EntstehungszeitraumBis = DateTime.Today.AddYears(-55),
                Schutzfristkategorie = ActaProClientValues.SchutzfristKategorieArt92,
                ZugaenglichkeitGemaessBga = ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                ZustaendigeStellenKeys = ["Key_1", "Key_2"],
            };

            // Act
            var result = provider.GetDownloadAccessTokens(data);

            // Assert
            return result.Count;
        }
    }
}

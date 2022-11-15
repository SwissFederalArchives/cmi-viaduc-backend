using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Common;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable PossibleNullReferenceException

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class ArchiveRecordBuilderTests
    {
        [SetUp]
        public void Setup()
        {
            var translationFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"sample.tab");
        }

        private readonly LanguageSettings languageSettings = new LanguageSettings {DefaultLanguage = new CultureInfo("de-CH")};
        private readonly ApplicationSettings applicationSettings = new ApplicationSettings {DigitalRepositoryElementIdentifier = "10367"};

        [Test]
        public void ArchiveRecord_Must_Have_5_Metadata_Values()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.Count.Should().Be(5);
        }

        [Test]
        public void ArchiveRecord_Must_Have_4_Field_Security_Values()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Security.FieldAccessToken.Count.Should().Be(1);
            result.Security.MetadataAccessToken.Count.Should().Be(1);
            result.Security.PrimaryDataDownloadAccessToken.Count.Should().Be(1);
            result.Security.PrimaryDataFulltextAccessToken.Count.Should().Be(1);
        }

        [Test]
        public void ArchiveRecord_SecurityField_FieldAccessToken_HasBAR_Value()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Security.FieldAccessToken[0].Should().Be("BAR");
        }

        [Test]
        public void ArchiveRecord_Has_Single_Memo_Field()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementType == DataElementElementType.memo).Should().Be(1);
        }

        [Test]
        public void ArchiveRecord_Memo_Field_Has_Single_Text_Value()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementType == DataElementElementType.memo).ElementValue.Count.Should().Be(1);
        }

        [Test]
        public void ArchiveRecord_Memo_Field_Has_Concatenated_Value_Of_Text_Parts()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementType == DataElementElementType.memo).ElementValue.First().TextValues
                .First(t => t.IsDefaultLang).Value.Should().Be("Some long text that continues on several lines to be stiched together ");
        }

        [Test]
        public void ArchiveRecord_Check_ArchiveplanContext_Field_Correct_set()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Display.ArchiveplanContext.Count.Should().Be(3);
            result.Display.ArchiveplanContext[0].Protected.Should().BeTrue();
            result.Display.ArchiveplanContext[1].Protected.Should().BeFalse();
        }


        [Test]
        public void ArchiveRecord_Signatur_Field_Is_Repeated_Field()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementId == ((int) ScopeArchivDatenElementId.Signatur).ToString()).Should().Be(1);
            result.Metadata.DetailData.FirstOrDefault(d => d.ElementId == ((int) ScopeArchivDatenElementId.Signatur).ToString()).ElementValue.Count
                .Should().Be(2);
        }

        [Test]
        public void ArchiveRecord_Signatur_Field_Has_One_Element()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.DetailData.Count(d => d.ElementId == ((int) ScopeArchivDatenElementId.Signatur).ToString()).Should().Be(1);
        }

        [Test]
        public void ArchiveRecord_Accession_Year_Is_Read_From_AccessionLink_Element()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");

            // Assert
            result.Metadata.AccessionDate.Should().Be(1950);
        }

        [Test]
        public void ArchiveRecord_Date_Range_Is_Correctly_Processed()
        {
            // Arrange
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var ab = new ArchiveRecordBuilder(provider, languageSettings, applicationSettings, new CachedLookupData(provider));

            // Act
            var result = ab.Build("1000");
            var dateRange = result.Metadata.DetailData.FirstOrDefault(d => d.ElementType == DataElementElementType.dateRange);

            // Assert
            dateRange.ElementValue.Count.Should().Be(1);
            var elementValue = dateRange.ElementValue.FirstOrDefault();
            elementValue.TextValues.First(t => t.IsDefaultLang).Value.Should().Be("1940 - ca. 1950");
            elementValue.TextValues.First(t => t.Lang == "en").Value.Should().Be("1940 - approx. 1950");
            elementValue.DateRange.DateOperator.Should().Be(DateRangeDateOperator.fromTo);
            elementValue.DateRange.From.Should().Be("+1940");
            elementValue.DateRange.To.Should().Be("+1950");
            elementValue.DateRange.FromDate.Should().Be(new DateTime(1940, 1, 1));
            elementValue.DateRange.ToDate.Should().Be(new DateTime(1950, 12, 31));
            elementValue.DateRange.FromApproxIndicator.Should().BeFalse();
            elementValue.DateRange.ToApproxIndicator.Should().BeTrue();
            elementValue.DateRange.SearchFromDate.Should().BeBefore(new DateTime(1940, 1, 1));
            elementValue.DateRange.SearchToDate.Should().BeAfter(new DateTime(1950, 12, 31));
        }
    }
}
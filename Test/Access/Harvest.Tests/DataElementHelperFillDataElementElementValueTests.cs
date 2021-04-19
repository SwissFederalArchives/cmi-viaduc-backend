using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Access.Harvest.Tests.Properties;
using CMI.Contract.Common;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable PossibleNullReferenceException

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class DataElementHelperFillDataElementElementValueTests
    {
        private readonly DetailDataDataSet ds = new DetailDataDataSet();
        private readonly CultureInfo defaultCulture = new CultureInfo("de-CH");
        private readonly CultureInfo englishCulture = new CultureInfo("en-US");
        private readonly CultureInfo frenchCulture = new CultureInfo("fr-FR");
        private readonly CultureInfo italianCulture = new CultureInfo("it-IT");

        private readonly LanguageSettings languageSettings = new LanguageSettings
        {
            DefaultLanguage = new CultureInfo("de-CH"),
            SupportedLanguages = new List<CultureInfo>
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR"),
                new CultureInfo("it-IT")
            }
        };

        [Test]
        public void Accrual_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 5;
            dataRow.MEMO_TXT = "Some Text";
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.accrual, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("5 - Some Text");
            value.TextValues.FirstOrDefault().Value.Should().Be("5 - Some Text");
        }


        [Test]
        public void Boolean_Data_Element_No()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 0;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.boolean, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Nein");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("No");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("Non");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("No");
            value.BooleanValue.Should().BeFalse();
        }

        [Test]
        public void Boolean_Data_Element_Yes()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.boolean, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Ja");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("Yes");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("Oui");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("Si");
            value.BooleanValue.Should().BeTrue();
        }

        [Test]
        public void Date_Data_Element_Precision_Day()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 07, 20);
            dataRow.END_DT = new DateTime(1940, 07, 20);
            dataRow.BGN_DT_STND = "+19400720";
            dataRow.DT_OPRTR_ID = 0;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.date, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("20.07.1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("7/20/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("20/07/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("20/07/1940");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.exact);
            value.DateRange.From.Should().Be("+19400720");
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeFalse();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 7, 20));
            value.DateRange.ToDate.Should().Be(new DateTime(1940, 7, 20));
            value.DateRange.SearchFromDate.Should().BeSameDateAs(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeSameDateAs(value.DateRange.ToDate);
        }

        [Test]
        public void Date_Data_Element_Precision_Month()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 07, 01);
            dataRow.END_DT = new DateTime(1940, 07, 31);
            dataRow.BGN_DT_STND = "+194007";
            dataRow.DT_OPRTR_ID = 0;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.date, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("07.1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("7/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("07/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("07/1940");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.exact);
            value.DateRange.From.Should().Be("+194007");
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeFalse();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 7, 1));
            value.DateRange.ToDate.Should().Be(new DateTime(1940, 7, 31));
            value.DateRange.SearchFromDate.Should().BeBefore(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeAfter(value.DateRange.ToDate);
        }

        [Test]
        public void Date_Data_Element_Precision_Year()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 01, 01);
            dataRow.END_DT = new DateTime(1940, 12, 31);
            dataRow.BGN_DT_STND = "+1940";
            dataRow.DT_OPRTR_ID = 0;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.date, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("1940");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.exact);
            value.DateRange.From.Should().Be("+1940");
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeFalse();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 1, 1));
            value.DateRange.ToDate.Should().Be(new DateTime(1940, 12, 31));
            value.DateRange.SearchFromDate.Should().BeBefore(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeAfter(value.DateRange.ToDate);
        }

        [Test]
        public void DateRange_Data_Element_Between_Precision_Mont_With_Begin_Circa()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 07, 01);
            dataRow.END_DT = new DateTime(1945, 08, 31);
            dataRow.BGN_DT_STND = "+194007";
            dataRow.END_DT_STND = "+194508";
            dataRow.BGN_CIRCA_IND = 1;
            dataRow.END_CIRCA_IND = 0;
            dataRow.DT_OPRTR_ID = (int) ScopeArchivDateOperator.Between;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.dateRange, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("zwischen ca. 07.1940 und 08.1945");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("between approx. 7/1940 and 8/1945");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("entre env. 07/1940 et 08/1945");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("tra il ca. 07/1940 e il 08/1945");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.between);
            value.DateRange.From.Should().Be("+194007");
            value.DateRange.To.Should().Be("+194508");
            value.DateRange.FromApproxIndicator.Should().BeTrue();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 7, 01));
            value.DateRange.ToDate.Should().Be(new DateTime(1945, 8, 31));
            value.DateRange.SearchFromDate.Should().BeBefore(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeAfter(value.DateRange.ToDate);
        }

        [Test]
        public void DateRange_Data_Element_Exact_Precision_Day()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 07, 20);
            dataRow.END_DT = new DateTime(1940, 07, 20);
            dataRow.BGN_DT_STND = "+19400720";
            dataRow.END_DT_STND = null;
            dataRow.BGN_CIRCA_IND = 0;
            dataRow.END_CIRCA_IND = 0;
            dataRow.DT_OPRTR_ID = (int) ScopeArchivDateOperator.Exact;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.dateRange, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("20.07.1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("7/20/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("20/07/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("20/07/1940");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.exact);
            value.DateRange.From.Should().Be("+19400720");
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeFalse();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 7, 20));
            value.DateRange.ToDate.Should().Be(new DateTime(1940, 7, 20));
            value.DateRange.SearchFromDate.Should().BeSameDateAs(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeSameDateAs(value.DateRange.ToDate);
        }

        [Test]
        public void DateRange_Data_Element_Exact_Precision_Mont_With_Circa()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 07, 01);
            dataRow.END_DT = new DateTime(1940, 07, 31);
            dataRow.BGN_DT_STND = "+194007";
            dataRow.END_DT_STND = null;
            dataRow.BGN_CIRCA_IND = 1;
            dataRow.END_CIRCA_IND = 0;
            dataRow.DT_OPRTR_ID = (int) ScopeArchivDateOperator.Exact;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.dateRange, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("ca. 07.1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("approx. 7/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("env. 07/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("ca. 07/1940");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.exact);
            value.DateRange.From.Should().Be("+194007");
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeTrue();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(new DateTime(1940, 7, 01));
            value.DateRange.ToDate.Should().Be(new DateTime(1940, 7, 31));
            value.DateRange.SearchFromDate.Should().BeBefore(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeAfter(value.DateRange.ToDate);
        }

        [Test]
        public void DateRange_Data_Element_Sine_Dato()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = DateTime.MinValue;
            dataRow.END_DT = DateTime.MaxValue;
            dataRow.BGN_DT_STND = null;
            dataRow.END_DT_STND = null;
            dataRow.BGN_CIRCA_IND = 0;
            dataRow.END_CIRCA_IND = 0;
            dataRow.DT_OPRTR_ID = (int) ScopeArchivDateOperator.SineDato;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.dateRange, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("s. d. (sine dato)");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("s. d. (sine dato)");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("s. d. (sans date)");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("s. d. (senza data)");
            value.DateRange.DateOperator.Should().Be(DateRangeDateOperator.sd);
            value.DateRange.From.Should().BeNullOrEmpty();
            value.DateRange.To.Should().BeNullOrEmpty();
            value.DateRange.FromApproxIndicator.Should().BeFalse();
            value.DateRange.ToApproxIndicator.Should().BeFalse();
            value.DateRange.FromDate.Should().Be(DateTime.MinValue);
            value.DateRange.ToDate.Should().Be(DateTime.MaxValue);
            value.DateRange.SearchFromDate.Should().BeSameDateAs(value.DateRange.FromDate);
            value.DateRange.SearchToDate.Should().BeSameDateAs(value.DateRange.ToDate);
        }

        [Test]
        public void Float_Data_Element_Two_Digits()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.FLOAT_ZAHL = 1012.5m;
            dataRow.DZML_STLN_ANZ = 2;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.@float, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be(dataRow.FLOAT_ZAHL.ToString("N2", defaultCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be(dataRow.FLOAT_ZAHL.ToString("N2", englishCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be(dataRow.FLOAT_ZAHL.ToString("N2", frenchCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be(dataRow.FLOAT_ZAHL.ToString("N2", italianCulture.NumberFormat));
            value.FloatValue.Value.Should().Be(1012.5f);
            value.FloatValue.DecimalPositions.Should().Be(2);
        }

        [Test]
        public void TestFormat()
        {
            const decimal decimalValue = 2001.5m;
            var result = decimalValue.ToString("N2", new CultureInfo("de-CH"));
            Assert.AreEqual(decimalValue.ToString("N2", defaultCulture), result, $"{result} Fehler beim formatieren");
            result.Should().Be(decimalValue.ToString("N2", defaultCulture));
        }

        [Test]
        public void DatePrecise_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.BGN_DT = new DateTime(1940, 05, 05);
            dataRow.END_DT = new DateTime(1940, 05, 05);
            dataRow.BGN_DT_STND = "+19400505";
            dataRow.END_DT_STND = null;
            dataRow.BGN_CIRCA_IND = 0;
            dataRow.END_CIRCA_IND = 0;
            dataRow.DT_OPRTR_ID = 0;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.datePrecise, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("05.05.1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("5/5/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("05/05/1940");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("05/05/1940");
            value.DateValue.Should().Be(new DateTime(1940, 5, 5));
        }

        [Test]
        public void Text_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Test value";
            dataRow.ELMNT_SQNZ_NR = 2;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.text, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Test value");
            value.Sequence.Should().Be(2);
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Should().BeNull();
        }

        [Test]
        public void Memo_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "continues here";
            dataRow.ELMNT_SQNZ_NR = 2;
            var value = new DataElementElementValue
            {
                TextValues = new List<DataElementElementValueTextValue>
                {
                    new DataElementElementValueTextValue {Value = "The sentence ", IsDefaultLang = true, Lang = "en"}
                }
            };

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.memo, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("The sentence continues here");
            value.Sequence.Should().Be(1);
        }

        [Test]
        public void Header_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.TITEL = "Titel";
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.header, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Titel");
            value.Sequence.Should().Be(1);
        }

        [Test]
        public void Selection_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Content of selection field";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.selection, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Content of selection field");
            value.Sequence.Should().Be(1);
            value.TextValues.Count.Should().Be(1);
        }

        [Test]
        public void Hyperlink_Data_Element_Old_Style()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "http://www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("http://www.microsoft.com");
            value.Link.Value.Should().Be("http://www.microsoft.com");
            value.Link.Href.Should().Be("http://www.microsoft.com");
        }

        [Test]
        public void Hyperlink_Data_Element_Old_Style_Https()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "https://www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("https://www.microsoft.com");
            value.Link.Value.Should().Be("https://www.microsoft.com");
            value.Link.Href.Should().Be("https://www.microsoft.com");
        }

        [Test]
        public void Hyperlink_Data_Element_Old_Style_No_Http_Prefix()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("www.microsoft.com");
            value.Link.Value.Should().Be("www.microsoft.com");
            value.Link.Href.Should().Be("http://www.microsoft.com");
        }

        [Test]
        public void Hyperlink_Data_Element_New_Style()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Microsoft" + Environment.NewLine + "http://www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Microsoft");
            value.Link.Value.Should().Be("Microsoft");
            value.Link.Href.Should().Be("http://www.microsoft.com");
        }

        [Test]
        public void Hyperlink_Data_Element_New_Style_Https()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Microsoft" + Environment.NewLine + "https://www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Microsoft");
            value.Link.Value.Should().Be("Microsoft");
            value.Link.Href.Should().Be("https://www.microsoft.com");
        }

        [Test]
        public void Hyperlink_Data_Element_New_Style_No_Http_Prefix()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Microsoft" + Environment.NewLine + "www.microsoft.com";
            dataRow.ELMNT_SQNZ_NR = 1;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.hyperlink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Microsoft");
            value.Link.Value.Should().Be("Microsoft");
            value.Link.Href.Should().Be("http://www.microsoft.com");
        }

        [Test]
        public void Integer_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 953125;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.integer, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be(dataRow.INT_ZAHL.ToString("N0", defaultCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be(dataRow.INT_ZAHL.ToString("N0", englishCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be(dataRow.INT_ZAHL.ToString("N0", frenchCulture.NumberFormat));
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be(dataRow.INT_ZAHL.ToString("N0", italianCulture.NumberFormat));
            value.IntValue.Should().Be(953125);
        }

        [Test]
        public void MailLink_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "mailto:info@microsoft.com";
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.mailLink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("mailto:info@microsoft.com");
            value.Link.Value.Should().Be("mailto:info@microsoft.com");
            value.Link.Href.Should().Be("mailto:info@microsoft.com");
        }

        [Test]
        public void MailLink_Data_Element_Without_Prefix()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "info@microsoft.com";
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.mailLink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("info@microsoft.com");
            value.Link.Value.Should().Be("info@microsoft.com");
            value.Link.Href.Should().Be("mailto:info@microsoft.com");
        }


        [Test]
        public void Time_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.ZT = new DateTime(1999, 01, 05, 02, 30, 34); // Date part is ignored
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.time, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("02:30:34");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("2:30:34 AM");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("02:30:34");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("02:30:34");
            value.TimeValue.Should().BeSameDateAs(new DateTime(1999, 01, 05, 02, 30, 34));
        }

        [Test]
        public void Timespan_Data_Element_More_Than_One_Day()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 1532145; // timespan in seconds
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.timespan, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("17:17:35:45");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("17:17:35:45");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("17:17:35:45");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("17:17:35:45");
            value.DurationInSeconds.Should().Be(1532145);
        }

        [Test]
        public void Timespan_Data_Element_Less_Than_A_Day()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.INT_ZAHL = 5721; // timespan in seconds
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.timespan, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("1:35:21");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Value.Should().Be("1:35:21");
            value.TextValues.FirstOrDefault(t => t.Lang == "fr").Value.Should().Be("1:35:21");
            value.TextValues.FirstOrDefault(t => t.Lang == "it").Value.Should().Be("1:35:21");
            value.DurationInSeconds.Should().Be(5721);
        }

        // Entity Link
        [Test]
        public void EntityLink_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = "Just a text";
            dataRow.VRKNP_GSFT_OBJ_ID = 1234;
            dataRow.VRKNP_GSFT_OBJ_KLS_ID = 9;
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.entityLink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be("Just a text");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Should().BeNull();
            value.EntityLink.Value.Should().Be("Just a text");
            value.EntityLink.EntityRecordId.Should().Be("1234");
            value.EntityLink.EntityType.Should().Be(Enum.GetName(typeof(ScopeArchivGeschaeftsObjektKlasse), 9));
        }

        // File Link
        [Test]
        public void FileLink_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = @"C:\Temp\testfile.txt";
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.fileLink, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be(@"C:\Temp\testfile.txt");
            value.TextValues.FirstOrDefault(t => t.Lang == "en").Should().BeNull();
        }


        // Image
        [Test]
        public void Image_and_Media_Data_Element()
        {
            // Arrange
            var dataRow = ds.DetailData.NewDetailDataRow();
            dataRow.MEMO_TXT = @"C:\Temp\testfile.jpg";
            dataRow.BNR_DATEN = (byte[]) new ImageConverter().ConvertTo(Resources.sample, typeof(byte[]));
            var value = new DataElementElementValue();

            // Act
            DataElementHelper.FillDataElementElementValue(DataElementElementType.image, dataRow, value, languageSettings);

            // Assert
            value.TextValues.FirstOrDefault(t => t.IsDefaultLang).Value.Should().Be(@"testfile.jpg");
            value.BlobValueBase64.Value.Should().Be(Convert.ToBase64String(dataRow.BNR_DATEN));
            value.BlobValueBase64.MimeType.Should().Be("image/jpeg");
        }
    }
}
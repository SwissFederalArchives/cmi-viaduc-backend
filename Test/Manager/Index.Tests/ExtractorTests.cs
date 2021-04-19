using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using CMI.Manager.Index.Config;
using CMI.Manager.Index.ValueExtractors;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    [TestFixture]
    public class ExtractorTests
    {
        [SetUp]
        public void Setup()
        {
            var dataElementFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataElements.json");
            var json = File.ReadAllText(dataElementFile);
            dataElements = JsonConvert.DeserializeObject<List<DataElement>>(json);

            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
            fieldsConfiguration = new CustomFieldsConfiguration(configFile);
        }

        private List<DataElement> dataElements;
        private CustomFieldsConfiguration fieldsConfiguration;

        [Test]
        public void TextExtractor_Should_Return_String()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetValue(dataElements, "1"); // 1 == Title

            actual.Length.Should().BeGreaterOrEqualTo(1);
            actual.Should().Contain("Flugzeug");
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Array()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetListValues(dataElements, "999"); // 999 == Custom Test field

            actual.GetType().Should().Be<List<string>>();
            actual.Count.Should().Be(2);
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Concatenated_String()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetValue(dataElements, "999"); // 999 == Custom Test field

            actual.Should().Be("Text 1Text 2");
        }

        [Test]
        public void DatePeriodExtractor_Returns_ElasticTimePeriod()
        {
            var extractor = new TimePeriodExtractor();
            var actual = extractor.GetValue(dataElements, "7"); // 7 == Entstehungszeitraum

            actual.Text.Should().Be("01.01.1914 - 31.12.1918");
            actual.EndDate.Year.Should().Be(1918);
            actual.StartDate.Year.Should().Be(1914);
            actual.Years.Count.Should().Be(5);
        }


        [Test]
        public void IntExtractor_Returns_Integer()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "1000");

            actual.Should().Be(1234);
        }

        [Test]
        public void IntExtractor_Returns_Integer_For_Timespan()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "1002");

            actual.Should().Be(1234);
        }

        [Test]
        public void FloatExtractor_Returns_ElasticFloat()
        {
            var extractor = new FloatExtractor();
            var actual = extractor.GetValue(dataElements, "1001");

            actual.DecimalPositions.Should().Be(2);
            actual.Value.Should().Be(12.5f);
            actual.Text.Should().Be("12.50");
        }

        [Test]
        public void BoolExtractor_Returns_Bool()
        {
            var extractor = new BoolExtractor();
            var actual = extractor.GetValue(dataElements, "1003");

            actual.Should().Be(true);
        }

        [Test]
        public void Base64Extractor_Returns_ElasticBase64()
        {
            var extractor = new Base64Extractor();
            var actual = extractor.GetValue(dataElements, "10");

            actual.Value.StartsWith("/9j/4AAQSkZJRgABA");
            actual.MimeType.Should().Be("image/jpeg");
        }

        [Test]
        public void HyperlinkExtractor_Returns_ElasticHyperlink()
        {
            var extractor = new HyperlinkExtractor();
            var actual = extractor.GetValue(dataElements, "10418");

            actual.Text.Should().Be("E27#1000/721#14093#5489* (Wikimedia Commons)");
            actual.Url.Should().Be("https://commons.wikimedia.org/wiki/File:Flugzeug_Grandjean_vor_dem_Aufstieg_-_CH-BAR_-_3236769.tif");
        }

        [Test]
        public void EntityLinkExtractor_Returns_ElasticEntityLink()
        {
            var extractor = new EntityLinkExtractor();
            var actual = extractor.GetValue(dataElements, "1004");

            actual.Value.Should().Be("Test");
            actual.EntityRecordId.Should().Be("100");
            actual.EntityType.Should().Be("type");
        }

        [Test]
        public void DateWithYearExtractor_Returns_ElasticDateWithYear()
        {
            var extractor = new DateWithYearExtractor();
            var actual = extractor.GetValue(dataElements, "1005");

            actual.Date.Should().Be(new DateTime(2017, 7, 13));
            actual.Year.Should().Be(2017);
        }
    }
}
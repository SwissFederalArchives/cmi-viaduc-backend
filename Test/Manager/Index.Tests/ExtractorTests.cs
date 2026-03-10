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
            var actual = extractor.GetValue(dataElements, "TITEL");

            actual.Length.Should().BeGreaterOrEqualTo(1);
            actual.Should().Contain("Flugzeug");
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Array()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetListValues(dataElements, "RepeatedFieldTest"); // 999 == Custom Test field

            actual.GetType().Should().Be<List<string>>();
            actual.Count.Should().Be(2);
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Concatenated_String()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetValue(dataElements, "RepeatedFieldTest"); // 999 == Custom Test field

            actual.Should().Be("Text 1Text 2");
        }

        [Test]
        public void DatePeriodExtractor_Returns_ElasticTimePeriod()
        {
            var extractor = new TimePeriodExtractor();
            var actual = extractor.GetValue(dataElements, "Entstehungszeitraum"); // 7 == 

            actual.Text.Should().Be("01.01.1914 - 31.12.1918");
            actual.EndDate.Year.Should().Be(1918);
            actual.StartDate.Year.Should().Be(1914);
            actual.Years.Count.Should().Be(5);
        }

        [Test]
        public void DatePeriodExtractor_SameDayDate_EndTimeIsEndOfDay()
        {
            var extractor = new TimePeriodExtractor();
            dataElements =
            [
                new()
                {
                    ElementName = "Entstehungszeitraum", ElementType = DataElementElementType.dateRange, ElementValue =
                    [
                        new()
                        {
                            DateRange = new DateRange()
                            {
                                DateOperator = DateRangeDateOperator.exact,
                                FromDate = new DateTime(2000, 1, 1),
                                ToDate = new DateTime(2000, 1, 1)
                            }
                        }
                    ]
                }
            ];
            var actual = extractor.GetValue(dataElements, "Entstehungszeitraum"); // 7 == 

            actual.StartDate.Should().Be(new DateTime(2000, 1,1, 0, 0, 0));
            actual.EndDate.Should().Be(new DateTime(2000, 1, 1, 23, 59, 59));
            actual.Years.Count.Should().Be(1);
        }

        [Test]
        public void DatePeriodExtractor_DateWithOneDayDifference_EndTimeIsEndOfDay()
        {
            var extractor = new TimePeriodExtractor();
            dataElements =
            [
                new()
                {
                    ElementName = "Entstehungszeitraum", ElementType = DataElementElementType.dateRange, ElementValue =
                    [
                        new()
                        {
                            DateRange = new DateRange()
                            {
                                DateOperator = DateRangeDateOperator.exact,
                                FromDate = new DateTime(2000, 1, 1),
                                ToDate = new DateTime(2000, 1, 2)
                            }
                        }
                    ]
                }
            ];
            var actual = extractor.GetValue(dataElements, "Entstehungszeitraum"); // 7 == 

            actual.StartDate.Should().Be(new DateTime(2000, 1, 1, 0, 0, 0));
            actual.EndDate.Should().Be(new DateTime(2000, 1, 2, 23, 59, 59));
            actual.Years.Count.Should().Be(1);
        }

        [Test]
        public void DatePeriodExtractor_DateWithTimes_KeepTime()
        {
            var extractor = new TimePeriodExtractor();
            dataElements =
            [
                new()
                {
                    ElementName = "Entstehungszeitraum", ElementType = DataElementElementType.dateRange, ElementValue =
                    [
                        new()
                        {
                            DateRange = new DateRange()
                            {
                                DateOperator = DateRangeDateOperator.exact,
                                FromDate = new DateTime(2000, 1, 1, 12, 15, 45),
                                ToDate = new DateTime(2000, 1, 2, 14, 15, 30)
                            }
                        }
                    ]
                }
            ];
            var actual = extractor.GetValue(dataElements, "Entstehungszeitraum"); // 7 == 

            actual.StartDate.Should().Be(new DateTime(2000, 1, 1, 12, 15, 45));
            actual.EndDate.Should().Be(new DateTime(2000, 1, 2, 14, 15, 30));
            actual.Years.Count.Should().Be(1);
        }

        [Test]
        public void IntExtractor_Returns_Integer()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "IntFieldTest");

            actual.Should().Be(1234);
        }

        [Test]
        public void IntExtractor_Returns_Integer_For_Timespan()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "TimespanFieldTest");

            actual.Should().Be(1234);
        }

        [Test]
        public void FloatExtractor_Returns_ElasticFloat()
        {
            var extractor = new FloatExtractor();
            var actual = extractor.GetValue(dataElements, "FloatFieldTest");

            actual.DecimalPositions.Should().Be(2);
            actual.Value.Should().Be(12.5f);
            actual.Text.Should().Be("12.50");
        }

        [Test]
        public void BoolExtractor_Returns_Bool()
        {
            var extractor = new BoolExtractor();
            var actual = extractor.GetValue(dataElements, "BoolFieldTest");

            actual.Should().Be(true);
        }

        [Test]
        public void Base64Extractor_Returns_ElasticBase64()
        {
            var extractor = new Base64Extractor();
            var actual = extractor.GetValue(dataElements, "BILD_ANSICHT");

            actual.Value.StartsWith("/9j/4AAQSkZJRgABA");
            actual.MimeType.Should().Be("image/jpeg");
        }

        [Test]
        public void HyperlinkExtractor_Returns_ElasticHyperlink()
        {
            var extractor = new HyperlinkExtractor();
            var actual = extractor.GetValue(dataElements, "DIGITALE_VERSION");

            actual.Text.Should().Be("E27#1000/721#14093#5489* (Wikimedia Commons)");
            actual.Url.Should().Be("https://commons.wikimedia.org/wiki/File:Flugzeug_Grandjean_vor_dem_Aufstieg_-_CH-BAR_-_3236769.tif");
        }

        [Test]
        public void EntityLinkExtractor_Returns_ElasticEntityLink()
        {
            var extractor = new EntityLinkExtractor();
            var actual = extractor.GetValue(dataElements, "EntityLink");

            actual.Value.Should().Be("Test");
            actual.EntityRecordId.Should().Be("100");
            actual.EntityType.Should().Be("type");
        }

        [Test]
        public void DateWithYearExtractor_Returns_ElasticDateWithYear()
        {
            var extractor = new DateWithYearExtractor();
            var actual = extractor.GetValue(dataElements, "DateWithYear");

            actual.Date.Should().Be(new DateTime(2017, 7, 13));
            actual.Year.Should().Be(2017);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using CMI.Manager.Index.Config;
using CMI.Manager.Index.ValueExtractors;
using Shouldly;
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

            actual.Length.ShouldBeGreaterThanOrEqualTo(1);
            actual.ShouldContain("Flugzeug");
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Array()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetListValues(dataElements, "RepeatedFieldTest"); // 999 == Custom Test field

            actual.ShouldBeOfType<List<string>>();
            actual.Count.ShouldBe(2);
        }

        [Test]
        public void TextExtractor_Repeated_Textfield_Should_Return_Concatenated_String()
        {
            var extractor = new TextExtractor();
            var actual = extractor.GetValue(dataElements, "RepeatedFieldTest"); // 999 == Custom Test field

            actual.ShouldBe("Text 1Text 2");
        }

        [Test]
        public void DatePeriodExtractor_Returns_ElasticTimePeriod()
        {
            var extractor = new TimePeriodExtractor();
            var actual = extractor.GetValue(dataElements, "Entstehungszeitraum"); // 7 == 

            actual.Text.ShouldBe("01.01.1914 - 31.12.1918");
            actual.EndDate.Year.ShouldBe(1918);
            actual.StartDate.Year.ShouldBe(1914);
            actual.Years.Count.ShouldBe(5);
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

            actual.StartDate.ShouldBe(new DateTime(2000, 1,1, 0, 0, 0));
            actual.EndDate.ShouldBe(new DateTime(2000, 1, 1, 23, 59, 59));
            actual.Years.Count.ShouldBe(1);
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

            actual.StartDate.ShouldBe(new DateTime(2000, 1, 1, 0, 0, 0));
            actual.EndDate.ShouldBe(new DateTime(2000, 1, 2, 23, 59, 59));
            actual.Years.Count.ShouldBe(1);
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

            actual.StartDate.ShouldBe(new DateTime(2000, 1, 1, 12, 15, 45));
            actual.EndDate.ShouldBe(new DateTime(2000, 1, 2, 14, 15, 30));
            actual.Years.Count.ShouldBe(1);
        }

        [Test]
        public void IntExtractor_Returns_Integer()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "IntFieldTest");

            actual.ShouldBe(1234);
        }

        [Test]
        public void IntExtractor_Returns_Integer_For_Timespan()
        {
            var extractor = new IntExtractor();
            var actual = extractor.GetValue(dataElements, "TimespanFieldTest");

            actual.ShouldBe(1234);
        }

        [Test]
        public void FloatExtractor_Returns_ElasticFloat()
        {
            var extractor = new FloatExtractor();
            var actual = extractor.GetValue(dataElements, "FloatFieldTest");

            actual.DecimalPositions.ShouldBe(2);
            actual.Value.ShouldBe(12.5f);
            actual.Text.ShouldBe("12.50");
        }

        [Test]
        public void BoolExtractor_Returns_Bool()
        {
            var extractor = new BoolExtractor();
            var actual = extractor.GetValue(dataElements, "BoolFieldTest");

            actual.ShouldBe(true);
        }

        [Test]
        public void Base64Extractor_Returns_ElasticBase64()
        {
            var extractor = new Base64Extractor();
            var actual = extractor.GetValue(dataElements, "BILD_ANSICHT");

            actual.Value.StartsWith("/9j/4AAQSkZJRgABA");
            actual.MimeType.ShouldBe("image/jpeg");
        }

        [Test]
        public void HyperlinkExtractor_Returns_ElasticHyperlink()
        {
            var extractor = new HyperlinkExtractor();
            var actual = extractor.GetValue(dataElements, "DIGITALE_VERSION");

            actual.Text.ShouldBe("E27#1000/721#14093#5489* (Wikimedia Commons)");
            actual.Url.ShouldBe("https://commons.wikimedia.org/wiki/File:Flugzeug_Grandjean_vor_dem_Aufstieg_-_CH-BAR_-_3236769.tif");
        }

        [Test]
        public void EntityLinkExtractor_Returns_ElasticEntityLink()
        {
            var extractor = new EntityLinkExtractor();
            var actual = extractor.GetValue(dataElements, "EntityLink");

            actual.Value.ShouldBe("Test");
            actual.EntityRecordId.ShouldBe("100");
            actual.EntityType.ShouldBe("type");
        }

        [Test]
        public void DateWithYearExtractor_Returns_ElasticDateWithYear()
        {
            var extractor = new DateWithYearExtractor();
            var actual = extractor.GetValue(dataElements, "DateWithYear");

            actual.Date.ShouldBe(new DateTime(2017, 7, 13));
            actual.Year.ShouldBe(2017);
        }
    }
}
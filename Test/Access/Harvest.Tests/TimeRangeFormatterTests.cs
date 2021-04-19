using System;
using System.Globalization;
using CMI.Access.Harvest.ScopeArchiv;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class TimeRangeFormatterTests
    {
        [Test]
        public void Format_Time_Range_Exact_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+19501214";
            var stndDateTo = "+19501214";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Exact, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("14.12.1950");
        }

        [Test]
        public void Format_Time_Range_Exact_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+19501214";
            var stndDateTo = "+19501214";

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Exact, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("12/14/1950");
        }

        [Test]
        public void Format_Time_Range_Between_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+195001";
            var stndDateTo = "+195501";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-DE"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Between, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("zwischen 01.1950 und 01.1955");
        }

        [Test]
        public void Format_Time_Range_Between_In_French_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+195001";
            var stndDateTo = "+195501";

            var formatter = new TimeRangeFormatter(new CultureInfo("fr-FR"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Between, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("entre 01/1950 et 01/1955");
        }

        [Test]
        public void Format_Time_Range_FromTo_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1950";
            var stndDateTo = "+1955";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-DE"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.FromTo, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("1950 - 1955");
        }

        [Test]
        public void Format_Time_Range_FromTo_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1950";
            var stndDateTo = "+1955";

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.FromTo, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("1950 - 1955");
        }

        [Test]
        public void Format_Time_Range_To_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1950";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-DE"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.To, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("bis 1950");
        }

        [Test]
        public void Format_Time_Range_To_In_Italian_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1950";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("it-IT"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.To, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("al 1950");
        }

        [Test]
        public void Format_Time_Range_After_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+19";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-DE"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.After, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("nach 19. Jh.");
        }

        [Test]
        public void Format_Time_Range_After_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+19";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.After, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("after 19th cent.");
        }

        [Test]
        public void Format_Time_Range_Before_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1936";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Before, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("vor 1936");
        }

        [Test]
        public void Format_Time_Range_Before_In_French_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+19";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("fr-FR"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Before, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("avant 19e s.");
        }

        [Test]
        public void Format_Time_Range_From_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+1936";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.From, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("ab 1936");
        }

        [Test]
        public void Format_Time_Range_From_In_Italian_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+193601";
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("it-IT"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.From, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("dal 01/1936");
        }

        [Test]
        public void Format_Time_Range_None_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = ""; // Irrelevant
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.None, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("keine Angabe");
        }


        [Test]
        public void Format_Time_Range_None_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = ""; // Irrelevant
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.None, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("No information given");
        }

        [Test]
        public void Format_Time_Range_SineDato_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = ""; // Irrelevant
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.SineDato, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("s. d. (sine dato)");
        }

        [Test]
        public void Format_Time_Range_SineDato_In_French_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = ""; // Irrelevant
            var stndDateTo = ""; // To date is ignored/irrelevant

            var formatter = new TimeRangeFormatter(new CultureInfo("fr-FR"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.SineDato, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("s. d. (sans date)");
        }

        [Test]
        public void Format_Time_Range_Before_Christ_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "-02";
            var stndDateTo = "-01";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.FromTo, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("2. Jh. v.Chr. - 1. Jh. v.Chr.");
        }

        [Test]
        public void Format_Time_Range_Before_Christ_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "-02";
            var stndDateTo = "-01";

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.FromTo, stndDateFrom, stndDateTo, false, false);

            // Assert
            result.Should().Be("2th cent. B.C. - 1th cent. B.C.");
        }

        [Test]
        public void Format_Single_Date_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+201205";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(stndDateFrom, false);

            // Assert
            result.Should().Be("05.2012");
        }

        [Test]
        public void Format_Single_Date_In_Italian_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+201205";

            var formatter = new TimeRangeFormatter(new CultureInfo("it-IT"));

            // Act
            var result = formatter.Format(stndDateFrom, false);

            // Assert
            result.Should().Be("05/2012");
        }

        [Test]
        public void Format_Single_Date_With_Circa_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+201205";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(stndDateFrom, true);

            // Assert
            result.Should().Be("ca. 05.2012");
        }

        [Test]
        public void Format_Time_Range_With_Circa_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+201205";
            var stndDateTo = "+2014";

            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Between, stndDateFrom, stndDateTo, true, true);

            // Assert
            result.Should().Be("zwischen ca. 05.2012 und ca. 2014");
        }

        [Test]
        public void Format_Time_Range_With_Circa_In_English_Returns_Correct_String()
        {
            // Arrange
            var stndDateFrom = "+201205";
            var stndDateTo = "+2014";

            var formatter = new TimeRangeFormatter(new CultureInfo("en-US"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Between, stndDateFrom, stndDateTo, true, true);

            // Assert
            result.Should().Be("between approx. 5/2012 and approx. 2014");
        }

        [Test]
        public void Format_Time_Range_With_Invalid_Culture_Throws_Exception()
        {
            // Arrange
            var action = new Action(() =>
            {
                var formatter = new TimeRangeFormatter(new CultureInfo("dummy"));
            });

            // Act

            // Assert
            action.Should().Throw<CultureNotFoundException>();
        }


        [Test]
        public void Format_Time_Range_With_Invalid_Dates_Returns_Empty_String()
        {
            // Arrange
            var stndDateFrom = "1950"; // Plus sign is missing
            var formatter = new TimeRangeFormatter(new CultureInfo("de-CH"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Exact, stndDateFrom, null, false, false);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Test]
        public void Format_Date_With_Neutral_Culture_Returns_Invariant_Date_Format()
        {
            // Arrange
            var stndDateFrom = "+19500512";
            var formatter = new TimeRangeFormatter(new CultureInfo("de"));

            // Act
            var result = formatter.Format(ScopeArchivDateOperator.Exact, stndDateFrom, null, false, false);

            // Assert
            result.Should().Be("1950.05.12");
        }
    }
}
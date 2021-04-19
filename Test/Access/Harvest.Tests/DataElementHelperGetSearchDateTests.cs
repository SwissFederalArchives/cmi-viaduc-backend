using System;
using CMI.Access.Harvest.ScopeArchiv;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class DataElementHelperGetSearchDateTests
    {
        [Test]
        public void Exact_Date_With_No_Circa_Returns_Same_Search_Dates()
        {
            // Arrange
            var startDateStd = "+19500101";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = false;
            var endDateStd = "+19601231";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = false;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate);
            result.ToDate.Should().Be(endDate);
        }

        [Test]
        public void Exact_Date_With_Circa_Returns_Same_Search_Dates()
        {
            // Arrange
            var startDateStd = "+19500101";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+19601231";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate);
            result.ToDate.Should().Be(endDate);
        }

        [Test]
        public void Date_With_Month_Precision_With_No_Circa_Returns_Half_a_Month_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+195001";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = false;
            var endDateStd = "+196012";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = false;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-15));
            result.ToDate.Should().Be(endDate.AddDays(15));
        }

        [Test]
        public void Date_With_Month_Precision_With_Circa_Returns_Half_a_Month_Times_6_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+195001";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+196012";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-15 * 6));
            result.ToDate.Should().Be(endDate.AddDays(15 * 6));
        }

        [Test]
        public void Date_With_Year_Precision_With_No_Circa_Returns_Half_a_Year_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+1950";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = false;
            var endDateStd = "+1960";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = false;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-180));
            result.ToDate.Should().Be(endDate.AddDays(180));
        }

        [Test]
        public void Date_With_Year_Precision_With_Circa_Returns_Half_a_Year_Times_4_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+1950";
            var startDate = new DateTime(1950, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+1960";
            var endDate = new DateTime(1960, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-180 * 4));
            result.ToDate.Should().Be(endDate.AddDays(180 * 4));
        }

        [Test]
        public void Date_With_Century_Precision_With_No_Circa_Returns_Half_a_Century_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+19";
            var startDate = new DateTime(1801, 01, 01);
            var startIsApprox = false;
            var endDateStd = "+19";
            var endDate = new DateTime(1900, 12, 31);
            var endIsApprox = false;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-365 * 50));
            result.ToDate.Should().Be(endDate.AddDays(365 * 50));
        }

        [Test]
        public void Date_With_Century_Precision_With_Circa_Returns_Half_a_Century_Times_2_Corrected_Search_Dates()
        {
            // Arrange
            var startDateStd = "+19";
            var startDate = new DateTime(1801, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+19";
            var endDate = new DateTime(1900, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-365 * 50 * 2));
            result.ToDate.Should().Be(endDate.AddDays(365 * 50 * 2));
        }

        [Test]
        public void Date_Resulting_In_Max_Date_Overflow_Returns_Max_Date()
        {
            // Arrange
            var startDateStd = "+1900";
            var startDate = new DateTime(1900, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+9999";
            var endDate = new DateTime(9999, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(startDate.AddDays(-180 * 4));
            result.ToDate.Should().Be(DateTime.MaxValue.Date);
        }

        [Test]
        public void Date_Resulting_In_Min_Date_Underrun_Returns_Min_Date()
        {
            // Arrange
            var startDateStd = "+0001";
            var startDate = new DateTime(0001, 01, 01);
            var startIsApprox = true;
            var endDateStd = "+1900";
            var endDate = new DateTime(1900, 12, 31);
            var endIsApprox = true;

            // Act
            var result = DataElementHelper.GetSearchDate(startDateStd, startDate, startIsApprox, endDateStd, endDate, endIsApprox);

            // Assert
            result.FromDate.Should().Be(DateTime.MinValue.Date);
            result.ToDate.Should().Be(endDate.AddDays(180 * 4));
        }
    }
}
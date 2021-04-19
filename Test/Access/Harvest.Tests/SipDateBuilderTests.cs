using System;
using CMI.Access.Harvest.ScopeArchiv;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class SipDateBuilderTests
    {
        [Test]
        public void If_AIS_date_is_null_then_return_keine_Angabe()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString(null, false, null, false, null, DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
        }

        [Test]
        public void If_AIS_from_date_is_century_then_ArgumentOutOfRange_exception_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            Action act = () =>
                sipBuilder.ConvertToValidSipDateString("+19", false, null, false, ScopeArchivDateOperator.Exact,
                    DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void If_AIS_from_date_is_year_month_then_ArgumentOutOfRange_exception_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            Action act = () =>
                sipBuilder.ConvertToValidSipDateString("+190010", false, null, false, ScopeArchivDateOperator.Exact,
                    DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void If_AIS_to_date_is_century_then_ArgumentOutOfRange_exception_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            Action act = () =>
                sipBuilder.ConvertToValidSipDateString("+1900", false, "+20", false, ScopeArchivDateOperator.FromTo,
                    DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void If_AIS_to_date_is_year_month_then_ArgumentOutOfRange_exception_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            Action act = () => sipBuilder.ConvertToValidSipDateString("+19001001", false, "+200001", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void If_AIS_date_is_sine_dato_then_return_keine_Angabe()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString(null, false, null, false, ScopeArchivDateOperator.SineDato,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
        }

        [Test]
        public void If_AIS_date_is_kA_then_return_keine_Angabe()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString(null, false, null, false, ScopeArchivDateOperator.None,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
        }

        [Test]
        public void T01_If_AIS_from_date_is_year_and_to_date_is_year_then_return_JJJJ_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1900", false, "+1950", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("1900-1950");
        }

        [Test]
        public void T02_If_AIS_from_date_is_year_and_operator_is_exact_then_return_JJJJ_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1900", false, null, false, ScopeArchivDateOperator.Exact,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("1900-1900");
        }

        [Test]
        public void T03_If_AIS_from_date_is_yearmonthday_and_operator_is_exact_then_return_TT_MM_JJJJ_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19000515", false, null, false, ScopeArchivDateOperator.Exact,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("15.05.1900-15.05.1900");
        }

        [Test]
        public void T04_If_AIS_from_date_is_yearmonthday_with_approx_indicator_and_operator_is_exact_then_return_ca_TT_MM_JJJJ_ca_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19000515", true, null, false, ScopeArchivDateOperator.Exact,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.15.05.1900-ca.15.05.1900");
        }

        [Test]
        public void T05_If_AIS_from_date_is_yearmonthday_with_approx_indicator_and_to_date_is_yearmonthday_then_return_ca_TT_MM_JJJJ_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19000515", true, "+19501030", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.15.05.1900-30.10.1950");
        }

        [Test]
        public void T06_If_AIS_from_date_is_yearmonthday_and_to_date_is_yearmonthday_with_approx_indicator_then_return_TT_MM_JJJJ_ca_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19000515", false, "+19501030", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("15.05.1900-ca.30.10.1950");
        }

        [Test]
        public void
            T07_If_AIS_from_date_is_yearmonthday_with_approx_indicator_and_to_date_is_yearmonthday_with_approx_indicator_then_return_ca_TT_MM_JJJJ_ca_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19000515", true, "+19501030", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.15.05.1900-ca.30.10.1950");
        }

        [Test]
        public void T08_If_AIS_from_date_is_year_with_approx_indicator_and_operator_is_exact_then_return_ca_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1900", true, null, false, ScopeArchivDateOperator.Exact,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.1900-ca.1900");
        }

        [Test]
        public void T09_If_AIS_from_date_is_year_with_approx_indicator_and_to_date_is_year_then_return_ca_JJJJ_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1750", true, "+1760", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.1750-1760");
        }

        [Test]
        public void T10_If_AIS_from_date_is_year_and_to_date_is_year_with_approx_indicator_then_return_JJJJ_ca_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1750", false, "+1760", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("1750-ca.1760");
        }

        [Test]
        public void T11_If_AIS_from_date_is_year_with_approx_indicator_and_to_date_is_year_with_approx_indicator_then_return_ca_JJJJ_ca_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1750", true, "+1760", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.1750-ca.1760");
        }

        [Test]
        public void T12_If_AIS_from_date_is_year_and_to_date_is_yearmonthday_then_return_JJJJ_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+2011", false, "+20150131", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("2011-31.01.2015");
        }

        [Test]
        public void T13_If_AIS_from_date_is_year_with_approx_indicator_and_to_date_is_yearmonthday_then_return_ca_JJJJ_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+2011", true, "+20150131", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.2011-31.01.2015");
        }

        [Test]
        public void T14_If_AIS_from_date_is_year_and_to_date_is_yearmonthday_with_approx_indicator_then_return_JJJJ_ca_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1999", false, "+20111111", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("1999-ca.11.11.2011");
        }

        [Test]
        public void
            T15_If_AIS_from_date_is_year_with_approx_indicator_and_to_date_is_yearmonthday_with_approx_indicator_then_return_ca_JJJJ_ca_TT_MM_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+1999", true, "+20111111", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.1999-ca.11.11.2011");
        }

        [Test]
        public void T16_If_AIS_from_date_is_yearmonthday_and_to_date_is_year_then_return_TT_MM_JJJJ_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+20111224", false, "+2017", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("24.12.2011-2017");
        }

        [Test]
        public void T17_If_AIS_from_date_is_yearmonthday_with_approx_indicator_and_to_date_is_year_then_return_ca_TT_MM_JJJJ_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+17150820", true, "+1716", false, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.20.08.1715-1716");
        }

        [Test]
        public void T18_If_AIS_from_date_is_yearmonthday_and_to_date_is_year_with_approx_indicator_then_return__TT_MM_JJJJ_ca_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+20111111", false, "+2013", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("11.11.2011-ca.2013");
        }

        [Test]
        public void
            T19_If_AIS_from_date_is_yearmonthday_with_approx_indicator_and_to_date_is_year_with_approx_indicator_then_return_ca_TT_MM_JJJJ_ca_JJJJ()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            var result = sipBuilder.ConvertToValidSipDateString("+19130412", true, "+1923", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            result.Should().Be("ca.12.04.1913-ca.1923");
        }

        [Test]
        public void If_invalid_scope_standard_dates_are_passed_then_ArgumentOutOfRangeException_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            // formats without algebraic sign
            Action action1 = () => sipBuilder.ConvertToValidSipDateString("19130412", true, "+1923", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);
            Action action2 = () => sipBuilder.ConvertToValidSipDateString("+19130412", true, "1923", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            action1.Should().Throw<ArgumentOutOfRangeException>();
            action2.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void If_from_date_is_newer_than_to_date_then_InvalidOperationException_is_thrown()
        {
            // Arrange
            var sipBuilder = new SipDateBuilder();

            // Act
            // formats without algebraic sign
            Action action1 = () =>
                sipBuilder.ConvertToValidSipDateString("+1930", true, "+1920", true, ScopeArchivDateOperator.FromTo,
                    DigitizationOrderBuilder.NoDataAvailable);
            Action action2 = () => sipBuilder.ConvertToValidSipDateString("+19130412", true, "+19001210", true, ScopeArchivDateOperator.FromTo,
                DigitizationOrderBuilder.NoDataAvailable);

            // Assert
            action1.Should().Throw<InvalidOperationException>();
            action2.Should().Throw<InvalidOperationException>();
        }
    }
}
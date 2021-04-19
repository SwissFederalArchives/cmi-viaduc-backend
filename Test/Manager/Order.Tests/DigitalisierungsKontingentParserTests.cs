using System;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class DigitalisierungsKontingentParserTests
    {
        [Test]
        public void EingabeInSingularShouldWork()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "1 Auftrag in 1 Tag";

            // act
            var result = sut.Parse(input);

            // assert
            result.AnzahlAuftraege.Should().Be(1);
            result.InAnzahlTagen.Should().Be(1);
        }

        [Test]
        public void EingabeInPluralShouldWork()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "2 Aufträge in 2 Tagen";

            // act
            var result = sut.Parse(input);

            // assert
            result.AnzahlAuftraege.Should().Be(2);
            result.InAnzahlTagen.Should().Be(2);
        }

        [Test]
        public void EingabeInPluralAndSingularShouldWork()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "2 Aufträge in 1 Tag";

            // act
            var result = sut.Parse(input);

            // assert
            result.AnzahlAuftraege.Should().Be(2);
            result.InAnzahlTagen.Should().Be(1);
        }


        [Test]
        public void EingabeInPluralAndSingularWithWhitespaceShouldWork()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "2    Aufträge      in 1 Tag";

            // act
            var result = sut.Parse(input);

            // assert
            result.AnzahlAuftraege.Should().Be(2);
            result.InAnzahlTagen.Should().Be(1);
        }

        [Test]
        public void InvalidRegexShouldThrowException()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "2 Paar Schuhe";

            // act
            var action = (Action) (() => { sut.Parse(input); });

            // assert
            action.Should().Throw<Exception>();
        }

        [Test]
        public void StringWithZerosShouldThrowException()
        {
            // arrange
            var sut = new DigitalisierungsKontingentParser();
            var input = "0 Aufträge in 1 Tag";

            // act
            var action = (Action) (() => { sut.Parse(input); });

            // assert
            action.Should().Throw<Exception>();
        }
    }
}
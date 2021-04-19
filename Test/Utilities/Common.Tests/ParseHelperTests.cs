using System;
using CMI.Utilities.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Utilities.Common.Tests
{
    [TestFixture]
    public class ParseHelperTests
    {
        [Test]
        public void ParseDateTimeSwissTest()
        {
            // ARRANGE
            const string parseValueSwissDate = "06.07.2018";
            const string parseValueUsDate = "08.31.2018"; // Dieses Datum sollte bekannt sein ;-)

            // ACT
            var resultParseValueSwissDate = parseValueSwissDate.ParseDateTimeSwiss();
            var resultParseValueSwissUsDate = parseValueUsDate.ParseDateTimeSwiss();

            // ASSERT
            resultParseValueSwissDate.Should().NotBeNull();
            resultParseValueSwissDate.Should().Be(new DateTime(2018, 7, 6));

            resultParseValueSwissUsDate.Should().BeNull();
        }

        [Test]
        public void ParseDateTimeSwissTextTest()
        {
            // ARRANGE
            const string parseValueText = "Denoshan";
            const string nullString = null;
            // ACT
            var resultParseValueText = parseValueText.ParseDateTimeSwiss();
            var resultParseValueNull = nullString.ParseDateTimeSwiss();
            var resultParseValueEmpty = string.Empty.ParseDateTimeSwiss();

            // ASSERT
            resultParseValueText.Should().BeNull();
            resultParseValueNull.Should().BeNull();
            resultParseValueEmpty.Should().BeNull();
        }
    }
}
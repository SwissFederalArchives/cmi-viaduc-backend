using System.Linq;
using CMI.Web.Frontend.api;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class TokenTests
    {
        [Test]
        public void Token_ein_Wort()
        {
            // ARRANGE
            const string eingabe = "Haus";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result.First().Text.Should().Be(eingabe);
            result.First().Index.Should().Be(0);
            result.First().Length.Should().Be(eingabe.Length);
        }

        [Test]
        public void Token_ein_Wort_mit_leerzeichen()
        {
            // ARRANGE
            const string eingabe = " Haus  ";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Count.Should().Be(1);

            result[0].Index.Should().Be(1);
            result[0].Length.Should().Be(4);
            result[0].Text.Should().Be("Haus");
        }

        [Test]
        public void Token_mehrere_Woerter()
        {
            // ARRANGE
            //                       1    2
            //                      0123456789012
            const string eingabe = "ein Text";
            var tokenList = new Tokenizer().GetTokens(eingabe).ToList();

            tokenList[0].Index.Should().Be(0);
            tokenList[0].Text.Should().Be("ein");
            tokenList[0].Length.Should().Be(3);

            tokenList[1].Index.Should().Be(4);
            tokenList[1].Text.Should().Be("Text");
            tokenList[1].Length.Should().Be(4);
            // ASSERT
        }

        [Test]
        public void Token_mehrere_Woerter_mit_leerzeichen()
        {
            // ARRANGE
            //                       1    2
            //                      0123456789012
            const string eingabe = " ein  Text   ";
            var tokenList = new Tokenizer().GetTokens(eingabe).ToList();

            tokenList[0].Index.Should().Be(1);
            tokenList[0].Text.Should().Be("ein");
            tokenList[0].Length.Should().Be(3);

            tokenList[1].Index.Should().Be(6);
            tokenList[1].Text.Should().Be("Text");
            tokenList[1].Length.Should().Be(4);
            // ASSERT
        }


        [Test]
        public void merge_Tokens()
        {
            // ARRANGE
            //                                1         2
            //                      01234567890123456789012
            const string eingabe = "   Habe   nur   Text   ";
            var tokenList = new Tokenizer().GetTokens(eingabe).ToList();

            // ACT
            var result = Token.MergeToToken(tokenList);

            // ASSERT
            result.Should().NotBeNull();
            result.Index.Should().Be(3);
            result.Text.Should().Be("Habe nur Text");
            result.Length.Should().Be(17);
        }

        [Test]
        public void merge_Tokens_mit_anfuehrungszeichen()
        {
            // ARRANGE
            //                                1         2
            //                      01234567890123456789012
            const string eingabe = "   \"Habe   nur   Text\"   Wort";
            var tokenList = new Tokenizer().GetTokens(eingabe).ToList();

            // ACT
            var result = Token.MergeToToken(tokenList);

            // ASSERT
            result.Should().NotBeNull();
            result.Index.Should().Be(3);
            result.Text.Should().Be("Habe nur Text Wort");
            result.Length.Should().Be(26);
        }

        [Test]
        public void get_Tokens_mit_anfuehrungszeichen()
        {
            // ARRANGE
            //                                1         2
            //                      012345678901234567890123456
            const string eingabe = "   \"Habe   nur   Text\"   ";
            var result = new Tokenizer().GetTokens(eingabe).ToList();


            // ASSERT
            result.Count.Should().Be(1);
            result.First().Should().NotBeNull();
            result.First().Index.Should().Be(3);
            result.First().Text.Should().Be("Habe nur Text");
            result.First().Length.Should().Be(19);
        }

        [Test]
        public void get_Tokens_mit_mehreren_anfuehrungszeichen()
        {
            //                      0 0000000001 11111111 1222222222 2333333333344444444
            // ARRANGE               0 1234567890 12345678 9012345678 9012345678901234567
            const string eingabe = " \"Ich   bin\" ein   \"Text  mit\" anführungszeichen";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(4);

            result[0].Text.Should().Be("Ich bin");
            result[0].Index.Should().Be(1);
            result[0].Length.Should().Be(11);

            result[1].Text.Should().Be("ein");
            result[1].Index.Should().Be(13);
            result[1].Length.Should().Be(3);

            result[2].Text.Should().Be("Text mit");
            result[2].Index.Should().Be(19);
            result[2].Length.Should().Be(11);

            result[3].Text.Should().Be("anführungszeichen");
            result[3].Index.Should().Be(31);
            result[3].Length.Should().Be(17);
        }

        [Test]
        public void ein_anfuehrungszeichen()
        {
            const string eingabe = "\"";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_mit_Blank()
        {
            const string eingabe = "\" ";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_mit_Blank_und_schlusszeichen()
        {
            const string eingabe = "\" \"";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_ohne_schlusszeichen()
        {
            const string eingabe = "\"hello";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result.First().Text.Should().Be("hello");
            result.First().Index.Should().Be(0);
            result.First().Length.Should().Be(6);
        }
    }
}
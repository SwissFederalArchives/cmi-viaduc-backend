using System.Linq;
using CMI.Web.Frontend.api;
using Shouldly;
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
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.First().Text.ShouldBe(eingabe);
            result.First().Index.ShouldBe(0);
            result.First().Length.ShouldBe(eingabe.Length);
        }

        [Test]
        public void Token_ein_Wort_mit_leerzeichen()
        {
            // ARRANGE
            const string eingabe = " Haus  ";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.Count.ShouldBe(1);

            result[0].Index.ShouldBe(1);
            result[0].Length.ShouldBe(4);
            result[0].Text.ShouldBe("Haus");
        }

        [Test]
        public void Token_mehrere_Woerter()
        {
            // ARRANGE
            //                       1    2
            //                      0123456789012
            const string eingabe = "ein Text";
            var tokenList = new Tokenizer().GetTokens(eingabe).ToList();

            tokenList[0].Index.ShouldBe(0);
            tokenList[0].Text.ShouldBe("ein");
            tokenList[0].Length.ShouldBe(3);

            tokenList[1].Index.ShouldBe(4);
            tokenList[1].Text.ShouldBe("Text");
            tokenList[1].Length.ShouldBe(4);
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

            tokenList[0].Index.ShouldBe(1);
            tokenList[0].Text.ShouldBe("ein");
            tokenList[0].Length.ShouldBe(3);

            tokenList[1].Index.ShouldBe(6);
            tokenList[1].Text.ShouldBe("Text");
            tokenList[1].Length.ShouldBe(4);
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
            result.ShouldNotBeNull();
            result.Index.ShouldBe(3);
            result.Text.ShouldBe("Habe nur Text");
            result.Length.ShouldBe(17);
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
            result.ShouldNotBeNull();
            result.Index.ShouldBe(3);
            result.Text.ShouldBe("Habe nur Text Wort");
            result.Length.ShouldBe(26);
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
            result.Count.ShouldBe(1);
            result.First().ShouldNotBeNull();
            result.First().Index.ShouldBe(3);
            result.First().Text.ShouldBe("Habe nur Text");
            result.First().Length.ShouldBe(19);
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
            result.ShouldNotBeNull();
            result.Count.ShouldBe(4);

            result[0].Text.ShouldBe("Ich bin");
            result[0].Index.ShouldBe(1);
            result[0].Length.ShouldBe(11);

            result[1].Text.ShouldBe("ein");
            result[1].Index.ShouldBe(13);
            result[1].Length.ShouldBe(3);

            result[2].Text.ShouldBe("Text mit");
            result[2].Index.ShouldBe(19);
            result[2].Length.ShouldBe(11);

            result[3].Text.ShouldBe("anführungszeichen");
            result[3].Index.ShouldBe(31);
            result[3].Length.ShouldBe(17);
        }

        [Test]
        public void ein_anfuehrungszeichen()
        {
            const string eingabe = "\"";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_mit_Blank()
        {
            const string eingabe = "\" ";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_mit_Blank_und_schlusszeichen()
        {
            const string eingabe = "\" \"";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        }

        [Test]
        public void ein_anfuehrungszeichen_ohne_schlusszeichen()
        {
            const string eingabe = "\"hello";

            // ACT
            var result = new Tokenizer().GetTokens(eingabe).ToList();

            // ASSERT
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.First().Text.ShouldBe("hello");
            result.First().Index.ShouldBe(0);
            result.First().Length.ShouldBe(6);
        }
    }
}
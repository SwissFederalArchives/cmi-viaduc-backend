using System.Collections.Generic;
using System.Linq;

namespace CMI.Web.Frontend.api
{
    public class Tokenizer
    {
        public IEnumerable<Token> GetTokens(string content)
        {
            char[] wordseparators = {' '};
            char[] quote = {'"'};
            var isQuote = false;
            Token currentToken = null;
            for (var index = 0; index < content.Length; index++)
            {
                var c = content[index];
                if (currentToken == null)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    if (quote.Contains(c))
                    {
                        isQuote = true;
                        currentToken = new Token {Index = index, Text = string.Empty, Length = 1};
                    }
                    else
                    {
                        currentToken = new Token {Index = index, Text = c.ToString(), Length = 1};
                    }
                }
                else
                {
                    if (quote.Contains(c))
                    {
                        isQuote = !isQuote;
                        currentToken.Length++;
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (!isQuote)
                        {
                            if (currentToken.Text != string.Empty)
                            {
                                yield return currentToken;
                            }

                            currentToken = null;
                        }
                        else if (!(currentToken.Text == string.Empty || currentToken.Text.EndsWith(" ")))
                        {
                            currentToken.Length++;
                            currentToken.Text += c;
                        }
                        else
                        {
                            currentToken.Length++;
                        }
                    }
                    else
                    {
                        currentToken.Length++;
                        currentToken.Text += c;
                    }
                }
            }

            if (currentToken != null && currentToken.Text != string.Empty)
            {
                yield return currentToken;
            }
        }
    }

    public class Token
    {
        public string Text { get; set; }

        public int Index { get; set; }

        public int Length { get; set; }

        public static Token MergeToToken(List<Token> tokenList)
        {
            tokenList = tokenList.OrderBy(t => t.Index).ToList();
            var firstToken = tokenList.First();
            var lastToken = tokenList.Last();
            return new Token
            {
                Index = firstToken.Index,
                Text = string.Join(" ", tokenList.Select(t => t.Text)),
                Length = lastToken.Index + lastToken.Length - firstToken.Index
            };
        }
    }
}
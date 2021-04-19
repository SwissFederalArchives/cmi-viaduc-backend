using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Web.Frontend.api.Interfaces;

namespace CMI.Web.Frontend.api
{
    public class SynonymFinder
    {
        private readonly int maxInputWords;
        private readonly IWoerterbuch woerterbuch;


        public SynonymFinder(IWoerterbuch woerterbuch, int maxInputWords)
        {
            this.woerterbuch = woerterbuch;
            this.maxInputWords = maxInputWords;
        }

        public SynonymTreffer[] GetSynonyme(string fieldContent, string language)
        {
            var returnValue = new List<SynonymTreffer>();

            if (string.IsNullOrWhiteSpace(fieldContent))
            {
                return new SynonymTreffer[0];
            }

            var tokenizer = new Tokenizer();
            var tokenList = tokenizer.GetTokens(fieldContent).ToList();

            if (tokenList.Count > maxInputWords)
            {
                return new SynonymTreffer[0];
            }

            var usedTokens = new List<Token>();
            foreach (var tokens in tokenList.GetCombinations())
            {
                if (tokens.Intersect(usedTokens).Any())
                {
                    continue;
                }

                var groups = Lookup(tokens, language);
                if (groups.Count > 0)
                {
                    usedTokens.AddRange(tokens);
                }

                returnValue.AddRange(groups);
            }

            return returnValue.ToArray();
        }

        private List<SynonymTreffer> Lookup(List<Token> tokenList, string language)
        {
            var result = new List<SynonymTreffer>();
            if (tokenList == null || !tokenList.Any())
            {
                return result;
            }

            var token = Token.MergeToToken(tokenList);
            var synonymGroups = woerterbuch.FindGroups(token.Text.ToLower());
            if (synonymGroups == null)
            {
                return result;
            }


            foreach (var synonymGroup in synonymGroups)
            {
                var treffer = string.Empty;
                var synonyme = new List<string>();

                foreach (var entry in synonymGroup.Entries)
                {
                    if (entry.Equals(token.Text, StringComparison.InvariantCultureIgnoreCase))
                    {
                        treffer = entry;
                    }
                    else
                    {
                        synonyme.Add(entry);
                    }
                }

                result.Add(new SynonymTreffer
                {
                    Index = token.Index,
                    Length = token.Length,
                    Treffer = treffer,
                    Synonyme = synonyme.ToArray(),
                    Quellen = synonymGroup.Sources.Select(source => $"{source.Key} - {source.GetName(language)}").ToArray()
                });
            }

            return result;
        }
    }

    public struct SynonymTreffer
    {
        public string Treffer { get; set; }

        public int Index { get; set; }

        public int Length { get; set; }

        public string[] Synonyme { get; set; }

        public string[] Quellen { get; set; }
    }
}
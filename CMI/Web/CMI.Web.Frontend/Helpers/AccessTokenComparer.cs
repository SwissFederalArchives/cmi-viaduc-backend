using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Web.Frontend.Helpers
{
    public class AccessTokenComparer
    {
        public static bool TokensMatch(AccessTokens calculated, AccessTokens elastic)
        {
            return Compare(calculated.MetadataAccessTokens, elastic.MetadataAccessTokens)
                && Compare(calculated.FulltextAccessTokens, elastic.FulltextAccessTokens)
                && Compare(calculated.DownloadAccessTokens, elastic.DownloadAccessTokens)
                && Compare(calculated.FieldAccessTokens, elastic.FieldAccessTokens);
        }

        private static bool Compare(string a, string b)
        {
            var normalize = new Func<string, HashSet<string>>(tokenStr =>
                new HashSet<string>(
                    tokenStr?.Split(',')
                             ?.Select(t => t.Trim())
                             ?.Where(t => !string.IsNullOrWhiteSpace(t) &&
                                          !t.StartsWith("EG_", StringComparison.OrdinalIgnoreCase) &&
                                          !t.StartsWith("FG_", StringComparison.OrdinalIgnoreCase))
                             ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase));

            var setA = normalize(a);
            var setB = normalize(b);

            return setA.SetEquals(setB);
        }
    }
}

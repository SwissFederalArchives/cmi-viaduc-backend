using System.Collections.Generic;
using System.Linq;

namespace CMI.Web.Frontend.api
{
    public static class Combinator
    {
        public static IEnumerable<List<T>> GetCombinations<T>(this IEnumerable<T> tokens)
        {
            var tokenList = tokens.ToList();
            for (var length = tokenList.Count; length > 0; length--)
            {
                foreach (var x in GetCombinationsWithLength(tokenList, length))
                {
                    yield return x;
                }
            }
        }

        internal static IEnumerable<List<T>> GetCombinationsWithLength<T>(List<T> tokenList, int length)
        {
            var firstElement = 0;
            while (firstElement + length <= tokenList.Count)
            {
                var l = new List<T>();
                l.AddRange(tokenList.Skip(firstElement).Take(length));
                yield return l;
                firstElement++;
            }
        }
    }
}
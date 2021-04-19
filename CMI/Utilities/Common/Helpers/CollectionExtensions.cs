using System.Collections.Generic;
using System.Linq;

namespace CMI.Utilities.Common.Helpers
{
    public static class CollectionExtensions
    {
        /// <summary>
        ///     Die Methode vereinfacht den Umgang mit Collections, die null sein können
        /// </summary>
        public static IEnumerable<T> IfNullReturnEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                return Enumerable.Empty<T>();
            }

            return source;
        }
    }
}
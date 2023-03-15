using System;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Engine.Asset.PostProcess;

public static class EnumerableExtensions
{
    public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
    {
        var result = source.SelectMany(selector).ToList();
        if (!result.Any())
        {
            return result;
        }

        return result.Concat(result.SelectManyRecursive(selector));
    }

    public static IEnumerable<T> SelectManyAllInclusive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
    {
        return source.Concat(source.SelectManyRecursive(selector));
    }
}
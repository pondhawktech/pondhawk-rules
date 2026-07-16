// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Pondhawk.Rules.Util;

[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
internal static class CombinatoricsExtensions
{
    internal static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
    {
        return k == 0 ? new[] { Array.Empty<T>() } : elements.SelectMany((e, i) => elements.Skip(i + 1).Combinations(k - 1).Select(c => (new[] { e }).Concat(c)));
    }

    internal static IEnumerable<IEnumerable<T>> CombinationsWithRepetition<T>(this IEnumerable<T> elements, int k)
    {
        return k == 0 ? new[] { Array.Empty<T>() } : elements.SelectMany((e, i) => elements.CombinationsWithRepetition(k - 1).Select(c => (new[] { e }).Concat(c)));
    }


    internal static IEnumerable<IEnumerable<T>> Variations<T>(this IEnumerable<T> elements, int k)
    {
        List<IEnumerable<T>> results = [];

        for (var x = 1; x <= k; x++)
        {
            foreach (var cmb in elements.Combinations(x))
                results.Add(cmb);
        }

        return results;
    }

    internal static IEnumerable<IEnumerable<T>> VariationsWithRepetition<T>(this IEnumerable<T> elements, int k)
    {
        List<IEnumerable<T>> results = [];

        for (var x = 1; x <= k; x++)
        {
            foreach (var cmb in elements.CombinationsWithRepetition(x))
                results.Add(cmb);
        }

        return results;
    }
}


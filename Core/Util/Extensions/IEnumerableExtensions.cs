using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Extensions;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Checks if the enumerable container is empty. This will only do the
    /// minimum iterations to determine if there is an element using the
    /// .Any() extension method.
    /// </summary>
    /// <param name="enumerable">The element to check.</param>
    /// <typeparam name="T">The type in the enumerable.</typeparam>
    /// <returns>True if it has no elements, false otherwise.</returns>
    public static bool Empty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();

    /// <summary>
    /// Filters out any null items, and converts a nullable enumerable into
    /// a non-nullable enumerable
    /// </summary>
    /// <param name="enumerable">The elements to enumerate over.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>An enumerable without nulls.</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
    {
        foreach (T? element in enumerable)
            if (element != null)
                yield return element;
    }

    /// <summary>
    /// Joins a collection together with a token.
    /// </summary>
    /// <param name="strings">The strings to join.</param>
    /// <param name="joinToken">The token to join with.</param>
    /// <returns>The resulting string from joining.</returns>
    public static string Join(this IEnumerable<string> strings, string joinToken)
    {
        return string.Join(joinToken, strings);
    }

    /// <summary>
    /// Enumerates a collection with the index.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="items">The items to iterate over.</param>
    /// <returns>A forward iteration with indices.</returns>
    public static IEnumerable<(int Index, T Element)> Enumerate<T>(this IEnumerable<T> items)
    {
        int index = 0;
        foreach (var item in items)
            yield return (index++, item);
    }
}

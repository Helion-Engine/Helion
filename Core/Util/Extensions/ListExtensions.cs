using System;
using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A list of extensions for ILists.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Checks if the list is empty or not.
        /// </summary>
        /// <param name="list">The list to check.</param>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <returns>True if it has no elements, false otherwise.</returns>
        public static bool Empty<T>(this IList<T> list) => list.Count == 0;
        
        /// <summary>
        /// Iterates over a list in reverse order. Does not make any temporary
        /// lists in the process.
        /// </summary>
        /// <param name="list">The list to iterate backwards over.</param>
        /// <param name="action">The action to perform for each element.
        /// </param>
        /// <typeparam name="T">The type of the list item.</typeparam>
        public static void ForEachReverse<T>(this IList<T> list, Action<T> action)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                action(list[i]);
        }

        /// <summary>
        /// Generates an enumeration of combinatoric pairs.
        /// </summary>
        /// <remarks>
        /// The pairs generated are every single combination of possible pairs
        /// with no repeats. For example 1234 generates 12, 13, 14, 23, 24, 34.
        /// </remarks>
        /// <param name="list">The list to generate pairs from.</param>
        /// <typeparam name="T">The list's generic type.</typeparam>
        /// <returns>An enumerable of all the combinations.</returns>
        public static IEnumerable<(T, T)> PairCombinations<T>(this IList<T> list)
        {
            return Generate();
            
            IEnumerable<(T, T)> Generate()
            {
                int length = list.Count;
                for (int firstIndex = 0; firstIndex < length; firstIndex++)
                    for (int secondIndex = firstIndex + 1; secondIndex < length; secondIndex++)
                        yield return (list[firstIndex], list[secondIndex]);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;

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
        /// Copies each element in the list and returns the new list.
        /// </summary>
        /// <remarks>
        /// This is a shallow copy and is intended primarily for primitive
        /// types in lists which we want to copy.
        /// </remarks>
        /// <typeparam name="T">The type of the list.</typeparam>
        /// <param name="list">The list to copy.</param>
        /// <returns>A new list of copied elements.</returns>
        public static IList<T> Copy<T>(this IList<T> list) => list.Select(element => element).ToList();
    }
}
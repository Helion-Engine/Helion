using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A list of extensions for linked lists.
    /// </summary>
    public static class LinkedListExtensions
    {
        /// <summary>
        /// Checks if the list is empty or not.
        /// </summary>
        /// <param name="list">The list to check.</param>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <returns>True if it has no elements, false otherwise.</returns>
        public static bool Empty<T>(this LinkedList<T> list) => list.Count == 0;
    }
}
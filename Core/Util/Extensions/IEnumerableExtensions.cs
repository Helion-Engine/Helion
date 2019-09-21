using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Extensions
{
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
    }
}
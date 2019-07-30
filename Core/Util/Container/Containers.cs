using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Container
{
    /// <summary>
    /// Contains helper functions for creating containers.
    /// </summary>
    public static class Containers
    {
        /// <summary>
        /// Creates an enumerable object from the elements provided, where all
        /// the null elements are removed.
        /// </summary>
        /// <typeparam name="T">The type for the container.</typeparam>
        /// <param name="elements">The elements to add.</param>
        /// <returns>A new enumerable container with no null elements from the
        /// arguments provided. If all the arguments are null then the returned
        /// list will have no elements.</returns>
        public static IList<T> WithoutNulls<T>(params T?[] elements) where T : class
        {
            return elements.Where(element => element != null).ToList();
        }
    }
}

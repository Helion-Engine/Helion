using System.Collections.Generic;

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
        /// <typeparam name="T">The type for the contanier.</typeparam>
        /// <param name="elements">The elements to add.</param>
        /// <returns>A new enumerable container with no null elements from the
        /// arguments provided. If all the arguments are null then the returned
        /// list will have no elements.</returns>
        public static IList<T> WithoutNulls<T>(params T?[] elements) where T : class
        {
            IList<T> list = new List<T>();
            foreach (T? element in elements)
                if (element != null)
                    list.Add(element);

            return list;
        }
    }
}

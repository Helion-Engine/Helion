using System;
using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A collection of enumerable extensions.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs an operation on each element, and returns the container
        /// reference that was passed in.
        /// </summary>
        /// <typeparam name="T">The container type.</typeparam>
        /// <param name="container">The container to operate on.</param>
        /// <param name="func">The function to apply to each element.</param>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> container, Action<T> func)
        {
            foreach (T item in container)
                func(item);

            return container;
        }
    }
}

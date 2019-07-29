using System;

namespace Helion.Util
{
    /// <summary>
    /// A helper class that provides a hashcode for a combination of elements.
    /// </summary>
    /// <remarks>
    /// This idea was inspired from:
    /// https://stackoverflow.com/questions/9036369/library-with-equals-and-gethashcode-helper-methods-for-net
    /// It is very easy to add more functions if needed for more parameters.
    /// </remarks>
    public static class HashCode
    {
        /// <summary>
        /// Gets the hashcode for the two elements.
        /// </summary>
        /// <param name="t1">The first element.</param>
        /// <param name="t2">The second element.</param>
        /// <typeparam name="T1">The first type.</typeparam>
        /// <typeparam name="T2">The second type.</typeparam>
        /// <returns>The hashcode for the elements.</returns>
        public static int Combine<T1, T2>(T1 t1, T2 t2) => Tuple.Create(t1, t2).GetHashCode();
        
        /// <summary>
        /// Gets the hashcode for the two elements.
        /// </summary>
        /// <param name="t1">The first element.</param>
        /// <param name="t2">The second element.</param>
        /// <param name="t3">The third element.</param>
        /// <typeparam name="T1">The first type.</typeparam>
        /// <typeparam name="T2">The second type.</typeparam>
        /// <typeparam name="T3">The third type.</typeparam>
        /// <returns>The hashcode for the elements.</returns>
        public static int Combine<T1, T2, T3>(T1 t1, T2 t2, T3 t3) => Tuple.Create(t1, t2, t3).GetHashCode();
    }
}
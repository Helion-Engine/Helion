using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A series of helper functions for a stack object.
    /// </summary>
    public static class StackExtensions
    {
        /// <summary>
        /// Checks if the stack is empty.
        /// </summary>
        /// <param name="stack">The stack to check.</param>
        /// <typeparam name="T">The stack generic type.</typeparam>
        /// <returns>True if it has no elements, false if not.</returns>
        public static bool Empty<T>(this Stack<T> stack) => stack.Count == 0;
        
        /// <summary>
        /// Checks to see if there is a value and populates an out variable
        /// with the result if so.
        /// </summary>
        /// <remarks>
        /// Emulates the .NET Standard 2.1 implementation.
        /// </remarks>
        /// <param name="stack">The stack to check.</param>
        /// <param name="value">The value to be set, or it is defaulted if the
        /// stack is empty.</param>
        /// <typeparam name="T">The stack generic type.</typeparam>
        /// <returns>True if the stack had a value, false if not.</returns>
        public static bool TryPeek<T>(this Stack<T> stack, out T value)
        {
            if (stack.Empty())
            {
                value = default;
                return false;
            }

            value = stack.Peek();
            return true;
        }
    }
}
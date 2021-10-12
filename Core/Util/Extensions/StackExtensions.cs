using System.Collections.Generic;

namespace Helion.Util.Extensions;

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
}


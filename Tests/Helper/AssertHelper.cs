using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Helper
{
    /// <summary>
    /// A series of helper functions for unit testing.
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Checks if two lists are equal. Elements are compared with the
        /// AreEqual assertion.
        /// </summary>
        /// <param name="first">The first list.</param>
        /// <param name="second">The second list.</param>
        /// <typeparam name="T">The type for each list.</typeparam>
        public static void ListEquals<T>(IList<T> first, IList<T> second)
        {
            if (first.Count != second.Count)
                Assert.Fail($"First length ({first.Count}) does not equal second length ({second.Count})");
            for (int i = 0; i < first.Count; i++)
                Assert.AreEqual(first[i], second[i], $"Failed on index {i}");
        }
    }
}
namespace Helion.Util.Extensions
{
    /// <summary>
    /// A collection of string helper functions.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks if the string has no characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns>True if it has no characters, false if it has one or more
        /// characters.</returns>
        public static bool Empty(this string str) => str.Length == 0;
    }
}
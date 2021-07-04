using System;
using System.Text.RegularExpressions;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A collection of string helper functions.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex MD5Regex = new(@"[0-9a-fA-F]{32}", RegexOptions.Compiled);
        
        /// <summary>
        /// Checks if the string has no characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns>True if it has no characters, false if it has one or more
        /// characters.</returns>
        public static bool Empty(this string str) => str.Length == 0;
        
        /// <summary>
        /// Checks if the string is an MD5 hash string: a 32 character long
        /// string of hexadecimal characters (case insensitive).
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if it's an MD5 hash string, false otherwise.
        /// </returns>
        public static bool IsMD5(this string text) => text.Length == 32 && MD5Regex.IsMatch(text);

        /// <summary>
        /// Checks two are equal, ignoring case sensitivity.
        /// </summary>
        /// <param name="text">The text to use.</param>
        /// <param name="other">The text to compare against.</param>
        /// <returns>True if so, false if not.</returns>
        public static bool EqualsIgnoreCase(this string text, string other)
        {
            return text.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
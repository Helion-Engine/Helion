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

        /// <summary>
        /// Checks if the string has at least one character.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns>True if it has one or more characters, false if it has 
        /// zero characters.</returns>
        public static bool NotEmpty(this string str) => !Empty(str);

        /// <summary>
        /// Converts it to an upper string.
        /// </summary>
        /// <remarks>
        /// This differs from the ToUpper() function because the type that is
        /// returned is ready to go for upper string usages. If it's done by 
        /// calling ToUpper() then when it's assigned to an UpperString object
        /// it will have to do another conversion on an already-upper-string.
        /// This avoids that double conversion.
        /// </remarks>
        /// <param name="str">The string to convert.</param>
        /// <returns>An upper string version.</returns>
        public static UpperString ToUpperString(this string str) => new UpperString(str);

        /// <summary>
        /// Gets a substring from the offset to the end of the string.
        /// </summary>
        /// <param name="str">The string being operated on.</param>
        /// <param name="offset">The starting offset. May be out of range, as
        /// it will return an empty string (and not throw).</param>
        /// <returns>The substring, or an empty string if the offset is out of
        /// range.</returns>
        public static string SubstringFrom(this string str, int offset)
        {
            if (offset < 0 || offset >= str.Length)
                return "";
            return str.Substring(offset, str.Length - offset);
        }
    }
}

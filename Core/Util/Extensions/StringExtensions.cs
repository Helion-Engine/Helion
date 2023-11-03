using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Helion.Util.Extensions;

/// <summary>
/// A collection of string helper functions.
/// </summary>
public static class StringExtensions
{
    private static readonly Regex MD5Regex = new(@"[0-9a-fA-F]{32}", RegexOptions.Compiled);
    private static readonly Regex NonUtf8Regex = new(@"[^\u0000-\u00FF]+", RegexOptions.Compiled);

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

    public static bool StartsWithIgnoreCase(this string text, string other)
    {
        return text.StartsWith(other, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EndsWithIgnoreCase(this string text, string other)
    {
        return text.EndsWith(other, StringComparison.OrdinalIgnoreCase);
    }

    public static bool GetLastFolder(this string path, out ReadOnlySpan<char> folder)
    {
        int endIndex = LastIndexOf(path, path.Length - 1, '/', '\\');
        if (endIndex == -1)
        {
            folder = new ReadOnlySpan<char>();
            return false;
        }

        int startIndex = LastIndexOf(path, endIndex - 1, '/', '\\');
        if (startIndex == endIndex)
        {
            folder = new ReadOnlySpan<char>();
            return false;
        }

        folder = path.AsSpan(startIndex + 1, endIndex - startIndex - 1);
        return true;
    }

    public static string StripNonUtf8Chars(this string str) =>
        NonUtf8Regex.Replace(str, string.Empty);

    private static int LastIndexOf(string text, int start, char value, char alt)
    {
        int startIndex = -1;
        for (int i = start; i >= 0; i--)
        {
            if (text[i] != value && text[i] != alt)
                continue;
            startIndex = i;
            break;
        }

        return startIndex;
    }

    // Returns a new string with spaces between words.
    // Ex: "CommandSlot1" -> "Command Slot 1"
    public static string WithWordSpaces(this string str, StringBuilder builder)
    {
        builder.Clear();
        if (str == "")
            return "";

        builder.Append(str[0]);

        for (int i = 1; i < str.Length; i++)
        {
            char prev = str[i - 1];
            char current = str[i];

            if (prev is >= 'a' and <= 'z' && current is < 'a' or > 'z')
                if (current != ' ')
                    builder.Append(' ');
            builder.Append(current);
        }

        return builder.ToString();
    }
}

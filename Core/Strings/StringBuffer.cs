using System;
using System.Collections.Generic;
namespace Helion.Strings;

// String buffer class that will concat and reuse buffers without allocations. NOT thread safe.
public unsafe static class StringBuffer
{
    private const int MinBufferLength = 64;
    private const char Null = '\0';
    private const char InitChar = ' ';

    private static readonly List<string> _strings = new();

    public static string Set(string str, string text)
    {
        str = EnsureBufferLength(str, text.Length, false);
        SetBuffer(str, text);

        return str;
    }

    public static string Append(string str, ReadOnlySpan<char> data)
    {
        int length = StringLength(str);
        int copyLength = data.Length;
        str = EnsureBufferLength(str, length + copyLength, true);
        fixed (char* to = str)
        {
            copyLength *= sizeof(char);
            fixed (char* from = data)
            {
                Buffer.MemoryCopy(from, to + length,
                    str.Length * sizeof(char) - (length * sizeof(char)),
                    copyLength);
            }

            to[length + data.Length] = Null;
        }
        return str;
    }

    public static string Append(string str, char c)
    {
        int length = StringLength(str);
        str = EnsureBufferLength(str, length + 1, true);
        fixed (char* buffer = str)
        {
            buffer[length] = c;
            buffer[length + 1] = Null;
        }
        return str;
    }

    public static string EnsureBufferLength(string str, int length, bool copy)
    {
        if (str.Length >= length)
            return str;

        string newString = GetString(length);
        if (copy)
            SetBuffer(newString, str);

        FreeString(str);
        return newString;
    }

    public static string GetString() => GetString(MinBufferLength);

    public static string GetString(int minLength)
    {
        for (int i = 0; i < _strings.Count; i++)
        {
            string str = _strings[i];
            if (str.Length >= minLength)
            {
                _strings.RemoveAt(i);
                Clear(str);
                return str;
            }
        }

        int amount = minLength / MinBufferLength;
        if (minLength % MinBufferLength != 0)
            amount += 1;

        string newString = new string(InitChar, MinBufferLength * amount);
        Clear(newString);
        return newString;
    }

    public static string GetStringExact(int length)
    {
        for (int i = 0; i < _strings.Count; i++)
        {
            string str = _strings[i];
            if (str.Length == length)
            {
                _strings.RemoveAt(i);
                return str;
            }
        }

        string newString = new string(InitChar, length);
        Clear(newString);
        return newString;
    }

    public static string ToStringExact(string str)
    {
        int length = StringLength(str);
        if (str.Length == length)
            return str;

        string newString = GetStringExact(length);
        SetBuffer(newString, str);
        return newString;
    }

    public static void Clear(string str)
    {
        fixed (char* buffer = str)
            buffer[0] = Null;
    }

    public static void FreeString(string str)
    {
        Clear(str);
        _strings.Add(str);
    }

    public static void ClearStringCache()
    {
        _strings.Clear();
    }

    public static int StringLength(string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == Null)
                return i;
        }

        return str.Length;
    }

    public static ReadOnlySpan<char> AsSpan(string str)
    {
        return str.AsSpan(0, StringLength(str));
    }

    private static void SetBuffer(string str, string text)
    {
        int copyLength = StringLength(text);
        fixed (char* to = str)
        {
            fixed (char* from = text)
                Buffer.MemoryCopy(from, to, str.Length * sizeof(char), copyLength * sizeof(char));
            to[text.Length] = Null;
        }
    }
}

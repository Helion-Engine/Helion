using System;
using System.Collections.Generic;

namespace Helion.Strings;

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

    public static string Append(string str, string text)
    {
        int length = StringLength(str);
        str = EnsureBufferLength(str,  length + StringLength(text), true);
        fixed (char* to = str)
        {
            fixed (char* from = text)
            {
                for (int i = 0; i < text.Length; i++)
                    to[length + i] = from[i];
            }

            to[length + text.Length] = Null;
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

    public static string Append(string str, int number, int pad = 0)
    {
        if (number == 0)
        {
            pad--;
            while (pad > 0)
            {
                Append(str, '0');
                pad--;
            }
            return Append(str, '0');
        }

        int length = StringLength(str);
        int addCount = 0;
        int countValue = number;
        while (countValue > 0)
        {
            addCount++;
            countValue /= 10;
        }

        pad -= addCount;
        while (pad > 0)
        {
            Append(str, '0');
            addCount++;
            pad--;
        }

        str = EnsureBufferLength(str, length + addCount, true);
        fixed (char* buffer = str)
        {
            if (number < 0)
            {
                buffer[length] = '-';
                addCount++;
            }

            int index = 0;
            int value = Math.Abs(number);
            while (value > 0)
            {
                int digit = (value % 10);
                value /= 10;
                buffer[length + addCount - index - 1] = (char)(digit + '0');
                index++;
            }

            buffer[length + addCount] = Null;
        }

        return str;
    }

    public static string EnsureBufferLength(string str, int length, bool copy)
    {
        if (str.Length >= length)
            return str;

        FreeString(str);
        string newString = GetString(length);
        if (copy)
            SetBuffer(newString, str);

        return newString;
    }

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

        string newString = new string(InitChar, Math.Max(minLength, MinBufferLength));
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
        Set(newString, str);
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

    public static int StringLength(string str)
    {
        fixed (char* buffer = str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == Null)
                    return i;
            }
        }

        return str.Length;
    }

    private static void SetBuffer(string str, string text)
    {
        fixed (char* to = str)
        {
            fixed (char* from = text)
            {
                for (int i = 0; i < text.Length; i++)
                    to[i] = from[i];
            }

            to[text.Length] = Null;
        }
    }
}

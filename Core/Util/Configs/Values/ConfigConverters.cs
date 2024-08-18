using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Helion.Util.Extensions;

namespace Helion.Util.Configs.Values;

/// <summary>
/// A collection of conversion functions that take some arbitrary input and
/// turn them into a desired type.
/// </summary>
public static class ConfigConverters
{
    internal static Func<object, T> MakeObjectToTypeConverterOrThrow<T>() where T : notnull
    {
        if (typeof(T) == typeof(bool))
            return MakeThrowableBoolConverter<T>();
        if (typeof(T) == typeof(int))
            return MakeThrowableIntConverter<T>();
        if (typeof(T) == typeof(double))
            return MakeThrowableDoubleConverter<T>();
        if (typeof(T) == typeof(string))
            return val => (T)(object)(val.ToString() ?? "");
        if (typeof(T).IsEnum)
            return MakeThrowableEnumConverter<T>();
        if (typeof(T) == typeof(List<string>))
            return MakeThrowableStringListConverter<T>();
        if (typeof(T) == typeof(FileInfo))
            return MakeThrowableFileInfoConverter<T>();

        // Last ditch attempt at a converter.
        MethodInfo? method = typeof(T).GetMethod("FromConfigString", BindingFlags.Static | BindingFlags.Public);
        if (method != null && method.ReturnType == typeof(T))
            return arg => ((T)method.Invoke(null, new[] { arg })!);

        throw new Exception($"No known way for config to convert type {typeof(T).Name}, add code to {nameof(ConfigConverters)} to fix this or add a 'public static {typeof(T).Name} FromConfigString(string s)' to the type");
    }

    private static Func<object, T> MakeThrowableBoolConverter<T>() where T : notnull
    {
        static T ThrowableBoolConverter(object obj)
        {
            string text = obj.ToString() ?? "false";
            if (text.EqualsIgnoreCase("true"))
                return (T)(object)true;
            if (Parsing.TryParseDouble(text, out double d))
                return (T)(object)(d != 0);
            return (T)(object)bool.Parse(text);
        }

        return ThrowableBoolConverter;
    }

    private static Func<object, T> MakeThrowableIntConverter<T>() where T : notnull
    {
        static T ThrowableIntConverter(object obj)
        {
            string text = obj.ToString() ?? "0";
            if (text.EqualsIgnoreCase("false"))
                return (T)(object)0;
            if (text.EqualsIgnoreCase("true"))
                return (T)(object)1;
            if (Parsing.TryParseDouble(text, out double d))
                return (T)(object)(int)d;
            return (T)(object)int.Parse(text);
        }

        return ThrowableIntConverter;
    }

    private static Func<object, T> MakeThrowableDoubleConverter<T>() where T : notnull
    {
        static T ThrowableDoubleConverter(object obj)
        {
            string text = obj.ToString() ?? "0";
            if (text.EqualsIgnoreCase("false"))
                return (T)(object)0.0;
            if (text.EqualsIgnoreCase("true"))
                return (T)(object)1.0;
            return (T)(object)Parsing.ParseDouble(text);
        }

        return ThrowableDoubleConverter;
    }
    private static Func<object, T> MakeThrowableEnumConverter<T>() where T : notnull
    {
        Array enumValues = Enum.GetValues(typeof(T));

        Dictionary<string, T> nameToEnum = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < enumValues.Length; i++)
        {
            object? enumValue = enumValues.GetValue(i);
            string? enumName = enumValue?.ToString();
            if (enumName != null && enumValue != null)
                nameToEnum[enumName] = (T)enumValue;
        }

        T ThrowableEnumConverter(object obj)
        {
            // If we're passed an integer, see if it's one of the enumerations.
            if (int.TryParse(obj.ToString(), out int enumNumber))
            {
                for (int i = 0; i < enumValues.Length; i++)
                {
                    int enumValue = (int)enumValues.GetValue(i)!;
                    if (enumValue == enumNumber)
                        return (T)(object)enumNumber;
                }
            }

            // Most of the time we will get a string, so look it up by name.
            string name = obj.ToString() ?? "";
            if (nameToEnum.TryGetValue(name, out T? value))
                return value;

            throw new Exception($"No such enum mapping for {obj} to {typeof(T).Name}");
        }

        return ThrowableEnumConverter;
    }

    private static Func<object, T> MakeThrowableStringListConverter<T>() where T : notnull
    {
        static T ThrowableStringListConverter(object obj)
        {
            // We store it in the format `"a", "bc", ...` so we need to wrap
            // it in []'s before letting the deserializer do the heavy lifting.
            string str = obj.ToString() ?? "[]";

            // Windows backslashes break the JSON parser, so convert them.
            str = str.Replace('\\', '/');

            List<string> elements = JsonSerializer.Deserialize<List<string>>(str) ??
                                    throw new Exception("List is malformed");
            return (T)(object)elements;
        }

        return ThrowableStringListConverter;
    }

    private static Func<object, T> MakeThrowableFileInfoConverter<T>() where T : notnull
    {
        static T ThrowableFileInfoConverter(object obj)
        {
            return (T)(object)new FileInfo(obj?.ToString() ?? string.Empty);
        }

        return ThrowableFileInfoConverter;
    }
}

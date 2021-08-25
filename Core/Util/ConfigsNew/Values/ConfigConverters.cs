using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Util.ConfigsNew.Values
{
    public static class ConfigConverters
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
                if (double.TryParse(text, out double d))
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
                if (double.TryParse(text, out double d))
                    return (T)(object)d;
                return (T)(object)int.Parse(text);
            }

            return ThrowableIntConverter;
        }
        
        private static Func<object, T> MakeThrowableDoubleConverter<T>() where T : notnull
        {
            static T ThrowableDoubleConverter(object obj)
            {
                string text = obj.ToString() ?? "0.0";
                if (text.EqualsIgnoreCase("false"))
                    return (T)(object)0.0;
                if (text.EqualsIgnoreCase("true"))
                    return (T)(object)1.0;
                return (T)(object)double.Parse(text);
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
                if (obj is int enumNumber)
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
                string str = $"[{obj.ToString() ?? ""}]";
                List<string> elements = JsonSerializer.Deserialize<List<string>>(str) ?? 
                                        throw new Exception("List is malformed");
                return (T)(object)elements;
            }

            return ThrowableStringListConverter;
        }

        internal static Func<T, string>? MakeToStringHelper<T>() where T : notnull
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                return null;
            
            if (typeof(T).IsSubclassOf(typeof(IList)))
            {
                return value =>
                {
                    if (value is not IList list)
                    {
                        Log.Error($"Config ToString() function misclassification for type {typeof(T).Name} as {value.GetType().Name}");
                        return "ERROR";
                    }
                    
                    List<object> enumerable = new();
                    foreach (object? obj in list)
                        if (obj != null)
                            enumerable.Add(obj);

                    return enumerable.Select(o => (o.ToString() ?? "").Trim()).Where(s => s != "").Join(", ");
                };
            }

            // All types that have `public string ToConfigString()` as a method
            // can be used as a config value for writing.
            MethodInfo? method = typeof(T).GetMethod("ToConfigString", BindingFlags.Public);
            if (method != null && method.ReturnType == typeof(string))
                return val => method.Invoke(val, null)?.ToString() ?? "";

            return null;
        }
    }
}

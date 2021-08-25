using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using static Helion.Util.Extensions.IEnumerableExtensions;

namespace Helion.Util.ConfigsNew.Values
{
    public static class ConfigConverters
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        internal static Func<object, T> MakeObjectToTypeConverterOrThrow<T>() where T : notnull
        {
            if (typeof(T) == typeof(bool))
            {
                // TODO
            }
            else if (typeof(T) == typeof(int))
            {
                // TODO
            }
            else if (typeof(T) == typeof(double))
            {
                // TODO
            }
            else if (typeof(T) == typeof(string))
            {
                // TODO
            }
            else if (typeof(T).IsEnum)
            {
                // TODO
            }
            else if (typeof(T) == typeof(List<string>))
            {
                // TODO
            }

            throw new Exception($"No known way for config to convert type {typeof(T)}, add code to {nameof(ConfigConverters)} to fix this");
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

            // All types that have `public static string ToConfigString()` as a method
            // can be used as a config value for writing.
            MethodInfo? method = typeof(T).GetMethod("ToConfigString", BindingFlags.Static | BindingFlags.Public);
            if (method != null && method.ReturnType == typeof(string))
                return val => method.Invoke(val, null)?.ToString() ?? "";

            return null;
        }
    }
}

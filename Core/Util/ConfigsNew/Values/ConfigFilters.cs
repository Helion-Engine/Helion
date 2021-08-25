using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.ConfigsNew.Values
{
    /// <summary>
    /// A collection of filters that are commonly used.
    /// </summary>
    public static class ConfigFilters
    {
        public static Func<E, E, E?> OnlyValidEnums<E>() where E : struct
        {
            if (!typeof(E).IsEnum)
                throw new Exception($"{typeof(E).Name} is supposed to be an enum for config filter {nameof(OnlyValidEnums)}");
            
            return (_, value) =>
            {
                // Can't do Enum.GetValues<E> since `where E : Enum` doesn't work
                // with Func<E, E, E?>'s third nullable parameter at the time of
                // writing.
                IEnumerable<E> enumValues = Enum.GetValues(typeof(E)).Cast<E>();
                foreach (E enumValue in enumValues)
                    if (value.Equals(enumValue))
                        return value;
                
                return null;
            };
        }

        public static Func<string, string, string?> NotEmpty() => (_, value) =>
        {
            return value != "" ? value : null;
        };

        public static Func<string, string, string?> IfEmptyThenSetTo(string resultIfEmpty)
        {
            if (resultIfEmpty == "")
                throw new Exception("Cannot have a non-empty config string result for a non-empty method");
            
            return (_, value) => value != "" ? value : resultIfEmpty;
        }

        public static Func<List<T>, List<T>, List<T>?> NonEmptyList<T>() => (_, value) =>
        {
            return value.Count == 0 ? value : null;
        };
        
        public static Func<int, int, int?> Min(int min)
        {
            return (_, value) => Math.Max(value, min);
        }
        
        public static Func<int, int, int?> Max(int max)
        {
            return (_, value) => Math.Min(value, max);
        }
        
        public static Func<int, int, int?> Clamp(int min, int max)
        {
            return (_, value) => Math.Clamp(value, min, max);
        }
        
        public static Func<double, double, double?> Min(double min)
        {
            return (_, value) => Math.Max(value, min);
        }
        
        public static Func<double, double, double?> Max(double max)
        {
            return (_, value) => Math.Min(value, max);
        }
        
        public static Func<double, double, double?> Clamp(double min, double max)
        {
            return (_, value) => Math.Clamp(value, min, max);
        }
        
        public static Func<double, double, double?> ClampNormalized()
        {
            return Clamp(0.0, 1.0);
        }
    }
}

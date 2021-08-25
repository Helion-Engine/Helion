using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.ConfigsNew.Values
{
    /// <summary>
    /// A collection of filters that are commonly used.
    /// </summary>
    public static class ConfigFilters
    {
        public static Func<E, E, bool> OnlyValidEnums<E>() where E : struct
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
                        return true;
                
                return false;
            };
        }

        public static readonly Func<string, string, bool> NotEmpty = (_, value) => value != "";

        public static Func<string, string> IfEmptyDefaultTo(string resultIfEmpty)
        {
            Precondition(resultIfEmpty != "", "Cannot have a non-empty config string result for a non-empty method");
            
            return value => value != "" ? value : resultIfEmpty;
        }

        public static Func<List<T>, List<T>, bool> NonEmptyList<T>() => (_, value) => value.Count > 0;

        public static Func<int, int, bool> Greater(int min) => (_, value) => value > min;
        
        public static Func<double, double, bool> Greater(double min) => (_, value) => value > min;
        
        public static Func<int, int> GreaterOrEqual(int min) => value => Math.Max(value, min);
        
        public static Func<double, double> GreaterOrEqual(double min) => value => Math.Max(value, min);
        
        public static Func<int, int, bool> Less(int min) => (_, value) => value < min;
        
        public static Func<double, double, bool> Less(double min) => (_, value) => value < min;
        
        public static Func<int, int> LessOrEqual(int max) => value => Math.Min(value, max);
        
        public static Func<double, double> LessOrEqual(double max) => value => Math.Min(value, max);
        
        public static Func<double, double> Clamp(double min, double max) => value => Math.Clamp(value, min, max);

        public static readonly Func<double, double> ClampNormalized = Clamp(0.0, 1.0);
    }
}

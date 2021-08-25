using System.Diagnostics.CodeAnalysis;

namespace Helion.Util.ConfigsNew.Values
{
    public static class ConfigConverter
    {
        public static bool TryConvert<T>(object obj, [NotNullWhen(true)] out T? converted)
        {
            // TODO
            
            converted = default;
            return false;
        }
    }
}

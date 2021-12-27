namespace Helion.Util.Extensions
{
    public static class NullableExtensions
    {
        // If either value is null then false. Otherwise the values will be compared normally.
        public static bool NullableEquals<T>(this T? thisValue, T? value) where T : struct
        {
            if (!thisValue.HasValue || !value.HasValue)
                return false;

            return thisValue.Equals(value);
        }
    }
}

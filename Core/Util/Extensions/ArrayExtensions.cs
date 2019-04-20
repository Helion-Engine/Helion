namespace Helion.Util.Extensions
{
    /// <summary>
    /// A list of extensions for arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Fills all the elements of the array with a value.
        /// </summary>
        /// <typeparam name="T">The type for the array.</typeparam>
        /// <param name="array">The array to fill.</param>
        /// <param name="element">The element to fill with.</param>
        public static void Fill<T>(this T[] array, T element)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = element;
        }
    }
}

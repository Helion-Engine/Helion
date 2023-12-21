using Force.Crc32;
using Helion.Util.Container;
using Helion.Window.Input;

namespace Helion.Util.Extensions;

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

    /// <summary>
    /// Calculates the CRC32 hash of a bunch of bytes.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>The hashed string.</returns>
    public static string CalculateCrc32(this byte[] bytes)
    {
        return Crc32Algorithm.Compute(bytes).ToString("x2").ToUpper();
    }

    public static bool Contains(this DynamicArray<Key> keys, Key key)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == key)
                return true;
        }

        return false;
    }

    public static void AddUnique(this DynamicArray<Key> keys, Key key)
    {
        if (!keys.Contains(key))
            keys.Add(key);
    }
}

namespace Helion.Util.RandomGenerators;

/// <summary>
/// A random number generating object.
/// </summary>
public interface IRandom
{
    /// <summary>
    /// Gets a random byte.
    /// </summary>
    /// <returns>A random byte.</returns>
    int NextByte();

    /// <summary>
    /// Gets a random byte in the range of [-255, 255].
    /// </summary>
    /// <returns>A random byte in the range of a +/- unsigned byte.</returns>
    int NextDiff();

    int RandomIndex { get; }

    IRandom Clone();
    IRandom Clone(int randomIndex);

    /// <summary>
    /// Gets a random 32-bit unsigned integer.
    /// </summary>
    /// <returns>A random 32-bit unsigned integer.</returns>
    uint GenUInt32()
    {
        var a = (uint)NextByte();
        var b = (uint)NextByte();
        var c = (uint)NextByte();
        var d = (uint)NextByte();
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

    /// <summary>
    /// Gets an unbiased random 32-bit unsigned integer, in the range [0, <paramref name="bound"/>).
    /// </summary>
    /// <param name="bound">The exclusive upper bound, which must not be 0.</param>
    /// <returns>A random 32-bit unsigned integer in the range [0, <paramref name="bound"/>).</returns>
    uint GenUInt32BoundExclusive(uint bound)
    {
        unchecked
        {
            var threshold = ((uint)-bound) % bound;
            while (true)
            {
                var gen = GenUInt32();
                if (gen >= threshold)
                {
                    return gen % bound;
                }
            }
        }
    }

    /// <summary>
    /// Gets an unbiased random 32-bit signed integer, in the range [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>A random 32-bit signed integer in the range [<paramref name="min"/>, <paramref name="max"/>].</returns>
    int GenInt32Range(int min, int max)
    {
        unchecked
        {
            if (max < min)
            {
                (max, min) = (min, max);
            }
            var lo = (uint)min;
            var hi = (uint)max;
            var bound = hi - lo + 1;
            if (bound == 0)
            {
                return (int)GenUInt32();
            }
            return (int)(GenUInt32BoundExclusive(bound) + lo);
        }
    }
}

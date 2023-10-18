using System;

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
}

using System;

namespace Helion.Util.RandomGenerators;

/// <summary>
/// A random number generating object.
/// </summary>
public interface IRandom : ICloneable
{
    /// <summary>
    /// Gets a random byte.
    /// </summary>
    /// <returns>A random byte.</returns>
    byte NextByte();

    /// <summary>
    /// Gets a random byte in the range of [-255, 255].
    /// </summary>
    /// <returns>A random byte in the range of a +/- unsigned byte.</returns>
    int NextDiff();
}

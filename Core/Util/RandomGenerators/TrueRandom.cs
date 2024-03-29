using System;

namespace Helion.Util.RandomGenerators;

/// <summary>
/// Implements a true randomness (unlike the Doom version which loops every
/// 256 elements).
/// </summary>
public class TrueRandom : IRandom
{
    private readonly Random m_random = new();

    public int NextByte() => m_random.Next(256) & 0xFF;

    public int NextDiff() => m_random.Next(256) - m_random.Next(256);

    public int RandomIndex => 0;

    public IRandom Clone() => new TrueRandom();
    public IRandom Clone(int randomIndex) => new TrueRandom();
}

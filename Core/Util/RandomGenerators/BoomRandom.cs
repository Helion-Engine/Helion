using System;

namespace Helion.Util.RandomGenerators;

public class BoomRandom : IRandom
{
    private uint m_seed;

    public int RandomIndex => (int)m_seed;

    public BoomRandom(uint seed)
    {
        m_seed = seed;
    }

    public BoomRandom()
    {
        m_seed = 1993 + (uint)(Environment.TickCount * 69069); // Nice
        m_seed = m_seed * 2 + 1;
    }

    public IRandom Clone()
    {
        return new BoomRandom(m_seed);
    }

    public IRandom Clone(int randomIndex)
    {
        return new BoomRandom((uint)randomIndex);
    }

    public int NextByte()
    {
        uint boom = m_seed;
        m_seed = boom * 1664525 + 221297 + 49 * 2;
        return (byte)((boom >> 20) & 255);
    }

    public int NextDiff()
    {
        return NextByte() - NextByte();
    }
}

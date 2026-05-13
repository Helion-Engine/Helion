using System;

namespace Helion.Util.RandomGenerators;

public static class RandomExtensions
{
    public static double NextAngle(this IRandom random) => random.NextByte() * (Math.PI / 128.0);

    public static int NextHitDice(this IRandom random, int amount) => (1 + random.NextByte() & 7) * amount;
}

using Helion.Util.RandomGenerators;

namespace Helion.World.Entities.Definition.Properties.Components;

public struct DamageRangeProperty
{
    public int Value;
    public bool Exact;

    /// <summary>
    /// Gets a random damage value using the random provided. If there is no
    /// randomness due to it being an exact damage, the random provider will
    /// not be used.
    /// </summary>
    /// <param name="random">The random provider.</param>
    /// <returns>A damage value.</returns>
    public int Get(IRandom random)
    {
        return Exact ? Value : Value * ((random.NextByte() % 8) + 1);
    }
}


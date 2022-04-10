using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightFireFlickerDoom : SectorSpecialBase
{
    public readonly short MinBright;
    public readonly short MaxBright;

    private readonly IRandom m_random;
    private int m_delay;

    public LightFireFlickerDoom(IWorld world, Sector sector, IRandom random, short minLightLevel)
        : base(world, sector)
    {
        m_random = random;
        MinBright = (short)(minLightLevel + 16);
        MaxBright = Sector.LightLevel;
    }

    public LightFireFlickerDoom(IWorld world, Sector sector, IRandom random, LightFireFlickerDoomModel model)
        : base(world, sector)
    {
        m_random = random;
        MinBright = model.Min;
        MaxBright = model.Max;
        m_delay = model.Delay;
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightFireFlickerDoomModel()
        {
            Min = MinBright,
            Max = MaxBright,
            Delay = m_delay
        };
    }

    public override SpecialTickStatus Tick()
    {
        if (m_delay > 0)
        {
            m_delay--;
            return SpecialTickStatus.Continue;
        }

        int change = (m_random.NextByte() & 3) * 16;

        if (Sector.LightLevel - change < MinBright)
            Sector.SetLightLevel(MinBright, World.Gametick);
        else
            Sector.SetLightLevel((short)(MaxBright - change), World.Gametick);

        m_delay = 4;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightFireFlickerDoom : SectorSpecialBase
{
    public readonly short MinBright;
    public readonly short MaxBright;

    public override bool OverrideEquals => true;

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
            World.SetSectorLightLevel(Sector, MinBright);
        else
            World.SetSectorLightLevel(Sector, (short)(MaxBright - change));

        m_delay = 4;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;

    public override bool Equals(object? obj)
    {
        if (obj is not LightFireFlickerDoom fire)
            return false;

        return fire.Sector.Id == Sector.Id &&
            fire.MinBright == MinBright &&
            fire.MaxBright == MaxBright &&
            fire.m_delay == m_delay;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

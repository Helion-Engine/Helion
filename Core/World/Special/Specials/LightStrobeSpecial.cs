using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightStrobeSpecial : SectorSpecialBase
{
    private readonly short m_maxBright;
    private readonly short m_minBright;
    private readonly int m_brightTics;
    private readonly int m_darkTics;
    private int m_delay;

    public short MaxBright => m_maxBright;
    public short MinBright => m_minBright;
    public int BrightTicks => m_brightTics;
    public int DarkTicks => m_darkTics;

    public override bool OverrideEquals => true;

    public LightStrobeSpecial(IWorld world, Sector sector, IRandom random, short minLightLevel, int brightTics, int darkTics, bool sync)
         : base(world, sector)
    {
        m_brightTics = brightTics;
        m_darkTics = darkTics;
        m_maxBright = sector.LightLevel;
        m_minBright = minLightLevel;

        if (m_minBright == m_maxBright)
            m_minBright = 0;

        if (!sync)
            m_delay = random.NextByte() & 0x07;
    }

    public LightStrobeSpecial(IWorld world, Sector sector, LightStrobeSpecialModel model)
         : base(world, sector)
    {
        m_brightTics = model.BrightTics;
        m_darkTics = model.DarkTics;
        m_maxBright = model.Max;
        m_minBright = model.Min;
        m_delay = model.Delay;
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightStrobeSpecialModel()
        {
            SectorId = Sector.Id,
            Max = m_maxBright,
            Min = m_minBright,
            BrightTics = m_brightTics,
            DarkTics = m_darkTics,
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

        if (Sector.LightLevel == m_maxBright)
        {
            World.SetSectorLightLevel(Sector, m_minBright);
            m_delay = m_darkTics;
        }
        else if (Sector.LightLevel == m_minBright)
        {
            World.SetSectorLightLevel(Sector, m_maxBright);
            m_delay = m_brightTics;
        }

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;

    public override bool Equals(object? obj)
    {
        if (obj is not LightStrobeSpecial light)
            return false;

        return light.Sector.Id == Sector.Id &&
            light.MinBright == MinBright &&
            light.MaxBright == MaxBright &&
            light.BrightTicks == BrightTicks &&
            light.DarkTicks == DarkTicks;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

using Helion.Models;
using Helion.Util;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightChangeSpecial : SectorSpecialBase
{
    private short m_lightLevel;
    private int m_step;
    private int m_min;
    private int m_max;

    public LightChangeSpecial(IWorld world, Sector sector, short lightLevel, int fadeTics)
        : base(world, sector)
    {
        Set(world, sector, lightLevel, fadeTics);
    }

    public LightChangeSpecial(IWorld world, Sector sector, LightChangeSpecialModel model)
        : base(world, sector)
    {
        m_lightLevel = model.Light;
        m_step = model.Step;
        m_min = model.Min;
        m_max = model.Max;
    }

    public void Set(IWorld world, Sector sector, short lightLevel, int fadeTics)
    {
        World = world;
        Sector = sector;

        m_lightLevel = lightLevel;

        if (fadeTics > 0)
            m_step = (m_lightLevel - Sector.LightLevel) / fadeTics;
        else
            m_step = m_lightLevel - Sector.LightLevel;

        if (m_step < 0)
        {
            m_min = m_lightLevel;
            m_max = sector.LightLevel;
        }
        else
        {
            m_min = sector.LightLevel;
            m_max = m_lightLevel;
        }
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightChangeSpecialModel()
        {
            SectorId = Sector.Id,
            Light = m_lightLevel,
            Step = m_step,
            Min = m_min,
            Max = m_max,
        };
    }

    public override SpecialTickStatus Tick()
    {
        int set = MathHelper.Clamp(Sector.LightLevel + m_step, m_min, m_max);
        World.SetSectorLightLevel(Sector, (short)set);

        if (set == m_lightLevel)
            return SpecialTickStatus.Destroy;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

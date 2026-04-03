using Helion.Models;
using Helion.World.Geometry.Sectors;
using System;

namespace Helion.World.Special.Specials;

public class LightChangeSpecial : SectorSpecialBase
{
    private int m_lightLevelStart;
    private int m_lightLevelEnd;
    private int m_fadeTicks;
    private int m_fadeTickEnd;

    public LightChangeSpecial(IWorld world, Sector sector, short lightLevel, int fadeTics)
        : base(world, sector)
    {
        Set(world, sector, lightLevel, fadeTics);
    }

    public LightChangeSpecial(IWorld world, Sector sector, in LightChangeSpecialModel model)
        : base(world, sector)
    {
        // Flag new vs legacy model
        if (model.SectorId < 0)
        {
            m_fadeTicks = model.Step;
            m_fadeTickEnd = model.Light;
            m_lightLevelStart = model.Min;
            m_lightLevelEnd = model.Max;
        }
        else
        {
            var targetLightLevel = model.Light;
            m_lightLevelStart = model.Min;
            m_lightLevelEnd = model.Max;
            m_fadeTickEnd = (model.Max - model.Min) / model.Step;
            m_fadeTicks = Math.Abs(sector.LightLevel - targetLightLevel) / model.Step;
        }
    }

    public void Set(IWorld world, Sector sector, short lightLevel, int fadeTics)
    {
        World = world;
        Sector = sector;

        m_lightLevelStart = sector.LightLevel;
        m_lightLevelEnd = lightLevel;
        m_fadeTickEnd = fadeTics;
    }

    public LightChangeSpecialModel ToSpecialModel()
    {
        return new()
        {
            SectorId = -Sector.Id,
            Step = m_fadeTicks,
            Light = m_fadeTickEnd,
            Min = m_lightLevelStart,
            Max = m_lightLevelEnd,
        };
    }

    public override SpecialTickStatus Tick()
    {
        m_fadeTicks++;
        var lightLevel = ((m_lightLevelEnd - m_lightLevelStart) * m_fadeTicks) / m_fadeTickEnd + m_lightLevelStart;
        World.SetSectorLightLevel(Sector, (short)lightLevel);

        if (m_fadeTicks == m_fadeTickEnd)
            return SpecialTickStatus.Destroy;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

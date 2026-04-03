using Helion.Models;
using Helion.World.Geometry.Sectors;

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
        m_fadeTicks = model.Ticks;
        m_fadeTickEnd = model.TickEnd;
        m_lightLevelStart = model.LightStart;
        m_lightLevelEnd = model.LightEnd;
    }

    public void Set(IWorld world, Sector sector, short lightLevel, int fadeTics)
    {
        World = world;
        Sector = sector;

        m_lightLevelStart = sector.LightLevel;
        m_lightLevelEnd = lightLevel;
        m_fadeTicks = 0;
        m_fadeTickEnd = fadeTics;
    }

    public LightChangeSpecialModel ToSpecialModel()
    {
        return new()
        {
            SectorId = Sector.Id,
            Ticks = m_fadeTicks,
            TickEnd = m_fadeTickEnd,
            LightStart = m_lightLevelStart,
            LightEnd = m_lightLevelEnd,
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

using Helion.Models;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightChangeSpecial : SectorSpecialBase
{
    private int m_lightLevelStart;
    private int m_lightLevelEnd;
    private int m_fadeTicks;
    private int m_fadeTickEnd;
    private bool m_cycle;

    public LightChangeSpecial(IWorld world, Sector sector, short lightLevel, int fadeTics)
        : base(world, sector)
    {
        Set(world, sector, lightLevel, fadeTics);
    }

    public LightChangeSpecial(IWorld world, Sector sector, short lightLevelMin, short lightLevelMax, int fadeTics)
        : base(world, sector)
    {
        Set(world, sector, lightLevelMin, lightLevelMax, fadeTics, true);
    }

    public LightChangeSpecial(IWorld world, Sector sector, in LightChangeSpecialModel model)
        : base(world, sector)
    {
        m_fadeTicks = model.Ticks;
        m_fadeTickEnd = model.TickEnd;
        m_lightLevelStart = model.LightStart;
        m_lightLevelEnd = model.LightEnd;
        m_cycle = model.Cycle;
    }

    public void Set(IWorld world, Sector sector, short lightLevel, int fadeTics)
    {
        Set(world, sector, sector.LightLevel, lightLevel, fadeTics, false);
    }

    public void Set(IWorld world, Sector sector, short lightLevelStart, short lightLevelEnd, int fadeTics)
    {
        Set(world, sector, lightLevelStart, lightLevelEnd, fadeTics, true);
    }

    private void Set(IWorld world, Sector sector, short lightLevelStart, short lightLevelEnd, int fadeTics, bool cycle)
    {
        World = world;
        Sector = sector;

        // UZDoom forces to the highest brightness
        if (cycle && lightLevelStart < lightLevelEnd)
            (lightLevelStart, lightLevelEnd) = (lightLevelEnd, lightLevelStart);

        m_lightLevelStart = lightLevelStart;
        m_lightLevelEnd = lightLevelEnd;
        m_fadeTicks = 0;
        m_fadeTickEnd = fadeTics;
        m_cycle = cycle;
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
            Cycle = m_cycle
        };
    }

    public override SpecialTickStatus Tick()
    {
        m_fadeTicks++;
        var lightLevel = ((m_lightLevelEnd - m_lightLevelStart) * m_fadeTicks) / m_fadeTickEnd + m_lightLevelStart;
        World.SetSectorLightLevel(Sector, (short)lightLevel);

        if (m_fadeTicks == m_fadeTickEnd)
        {
            if (!m_cycle)
                return SpecialTickStatus.Destroy;

            (m_lightLevelStart, m_lightLevelEnd) = (m_lightLevelEnd, m_lightLevelStart);
            m_fadeTicks = 0;
        }

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

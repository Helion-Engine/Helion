using Helion.Models;
using Helion.Util;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightPulsateSpecial : SectorSpecialBase
{
    private const int DoomPulsateValue = 8;

    private readonly short m_maxBright;
    private readonly short m_minBright;
    private int m_inc;

    public LightPulsateSpecial(IWorld world, Sector sector, short minLightLevel)
         : base(world, sector)
    {
        m_maxBright = sector.LightLevel;
        m_minBright = minLightLevel;

        m_inc = -DoomPulsateValue;
    }

    public LightPulsateSpecial(IWorld world, Sector sector, LightPulsateSpecialModel model)
         : base(world, sector)
    {
        m_maxBright = model.Max;
        m_minBright = model.Min;
        m_inc = model.Inc;
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightPulsateSpecialModel()
        {
            SectorId = Sector.Id,
            Max = m_maxBright,
            Min = m_minBright,
            Inc = m_inc
        };
    }

    public override SpecialTickStatus Tick()
    {
        int lightLevel = Sector.LightLevel + m_inc;
        lightLevel = MathHelper.Clamp(lightLevel, m_minBright, short.MaxValue);
        Sector.SetLightLevel((short)lightLevel, World.Gametick);

        if ((m_inc < 0 && Sector.LightLevel <= m_minBright) || (m_inc > 0 && Sector.LightLevel >= m_maxBright))
            m_inc = -m_inc;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

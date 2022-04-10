using Helion.Models;
using Helion.Util;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightPulsateSpecial : SectorSpecialBase
{
    private const int DoomPulsateValue = 8;

    public readonly short MaxBright;
    public readonly short MinBright;
    private int m_inc;

    public LightPulsateSpecial(IWorld world, Sector sector, short minLightLevel)
         : base(world, sector)
    {
        MaxBright = sector.LightLevel;
        MinBright = minLightLevel;

        m_inc = -DoomPulsateValue;
    }

    public LightPulsateSpecial(IWorld world, Sector sector, LightPulsateSpecialModel model)
         : base(world, sector)
    {
        MaxBright = model.Max;
        MinBright = model.Min;
        m_inc = model.Inc;
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightPulsateSpecialModel()
        {
            SectorId = Sector.Id,
            Max = MaxBright,
            Min = MinBright,
            Inc = m_inc
        };
    }

    public override SpecialTickStatus Tick()
    {
        int lightLevel = Sector.LightLevel + m_inc;
        lightLevel = MathHelper.Clamp(lightLevel, MinBright, short.MaxValue);
        Sector.SetLightLevel((short)lightLevel, World.Gametick);

        if ((m_inc < 0 && Sector.LightLevel <= MinBright) || (m_inc > 0 && Sector.LightLevel >= MaxBright))
            m_inc = -m_inc;

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

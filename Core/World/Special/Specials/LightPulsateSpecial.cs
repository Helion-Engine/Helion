using Helion.Models;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightPulsateSpecial : SectorSpecialBase
{
    private const int DoomPulsateValue = 8;

    public readonly short MaxBright;
    public readonly short MinBright;
    private int m_inc;

    public override bool OverrideEquals => true;

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
        short lightLevel = (short)(Sector.LightLevel + m_inc);
        if ((m_inc < 0 && lightLevel <= MinBright) || (m_inc > 0 && lightLevel >= MaxBright))
        {
            m_inc = -m_inc;
            lightLevel = (short)(lightLevel + m_inc);
        }

        World.SetSectorLightLevel(Sector, lightLevel);
        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;

    public override bool Equals(object? obj)
    {
        if (obj is not LightPulsateSpecial light)
            return false;

        return light.Sector.Id == Sector.Id &&
            light.MinBright == MinBright &&
            light.MaxBright == MaxBright &&
            light.m_inc == m_inc;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

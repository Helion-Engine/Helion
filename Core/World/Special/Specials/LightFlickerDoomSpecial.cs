using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class LightFlickerDoomSpecial : SectorSpecialBase
{
    public readonly short MaxBright;
    public readonly short MinBright;
    public int Delay { get; private set; }

    private readonly IRandom m_random;

    public LightFlickerDoomSpecial(IWorld world, Sector sector, IRandom random, short minLightLevel)
         : base(world, sector)
    {
        m_random = random;
        MaxBright = sector.LightLevel;
        MinBright = minLightLevel;
    }

    public LightFlickerDoomSpecial(IWorld world, Sector sector, IRandom random, LightFlickerDoomSpecialModel model)
         : base(world, sector)
    {
        m_random = random;
        MaxBright = model.Max;
        MinBright = model.Min;
        Delay = model.Delay;
    }

    public override ISpecialModel? ToSpecialModel()
    {
        return new LightFlickerDoomSpecialModel()
        {
            SectorId = Sector.Id,
            Max = MaxBright,
            Min = MinBright,
            Delay = Delay
        };
    }

    public override SpecialTickStatus Tick()
    {
        if (Delay > 0)
        {
            Delay--;
            return SpecialTickStatus.Continue;
        }

        if (Sector.LightLevel == MaxBright)
        {
            Sector.SetLightLevel(MinBright, World.Gametick);
            Delay = (m_random.NextByte() & 7) + 1;
        }
        else
        {
            Sector.SetLightLevel(MaxBright, World.Gametick);
            Delay = (m_random.NextByte() & 31) + 1;
        }

        return SpecialTickStatus.Continue;
    }

    public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
}

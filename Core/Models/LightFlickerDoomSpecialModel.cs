using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class LightFlickerDoomSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public short Max { get; set; }
    public short Min { get; set; }
    public int Delay { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new LightFlickerDoomSpecial(world.Sectors[SectorId], world.Random, this);
    }
}


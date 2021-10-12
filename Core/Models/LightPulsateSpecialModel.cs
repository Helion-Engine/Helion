using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class LightPulsateSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public short Max { get; set; }
    public short Min { get; set; }
    public int Inc { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new LightPulsateSpecial(world.Sectors[SectorId], this);
    }
}

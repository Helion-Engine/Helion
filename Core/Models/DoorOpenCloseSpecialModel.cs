using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class DoorOpenCloseSpecialModel : SectorMoveSpecialModel
{
    public int Key { get; set; }

    public override ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new DoorOpenCloseSpecial(world, world.Sectors[SectorId], this);
    }
}

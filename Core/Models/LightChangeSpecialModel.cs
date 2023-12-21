using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class LightChangeSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public short Light { get; set; }
    public int Step { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new LightChangeSpecial(world, world.Sectors[SectorId], this);
    }
}

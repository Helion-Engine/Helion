using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public struct LightChangeSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public short Light { get; set; }
    public int Step { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    public readonly ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new LightChangeSpecial(world, world.Sectors[SectorId], this);
    }
}

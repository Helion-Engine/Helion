using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using System;

namespace Helion.Models;

public struct LightChangeSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public int Light { get; set; }
    public int Step { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    public readonly ISpecial? ToWorldSpecial(IWorld world)
    {
        var sectorId = Math.Abs(SectorId);
        if (!world.IsSectorIdValid(sectorId))
            return null;

        return new LightChangeSpecial(world, world.Sectors[sectorId], this);
    }
}

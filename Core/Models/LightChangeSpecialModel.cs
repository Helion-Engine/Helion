using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public struct LightChangeSpecialModel : ISpecialModel
{
    public int SectorId { get; set; }
    public int LightStart { get; set; }
    public int LightEnd { get; set; }
    public int Ticks { get; set; }
    public int TickEnd { get; set; }
    public bool Cycle { get; set;  }

    public readonly ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        return new LightChangeSpecial(world, world.Sectors[SectorId], this);
    }
}

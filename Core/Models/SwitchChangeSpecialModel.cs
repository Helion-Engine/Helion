using Helion.World;
using Helion.World.Special;

namespace Helion.Models;

public struct SwitchChangeSpecialModel : ISpecialModel
{
    public int LineId { get; set; }
    public bool Repeat { get; set; }
    public int Tics { get; set; }

    public readonly ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsLineIdValid(LineId))
            return null;

        return world.DataCache.GetSwitchChangeSpecial(world, world.Lines[LineId], this);
    }
}

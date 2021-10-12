using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class SwitchChangeSpecialModel : ISpecialModel
{
    public int LineId { get; set; }
    public bool Repeat { get; set; }
    public int Tics { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsLineIdValid(LineId))
            return null;

        return new SwitchChangeSpecial(world, world.Lines[LineId], this);
    }
}

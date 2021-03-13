using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using System;
using System.Collections.Generic;

namespace Helion.Models
{
    public class StairSpecialModel : ISpecialModel
    {
        public int Delay { get; set; }
        public double StartZ { get; set; }
        public int Destroy { get; set; }
        public int DelayTics { get; set; }
        public bool Crush { get; set; }
        public IList<int> SectorIds { get; set; } = Array.Empty<int>();
        public IList<int> Heights { get; set; } = Array.Empty<int>();
        public SectorMoveSpecialModel MoveSpecial { get; set; }

        public ISpecial? ToWorldSpecial(IWorld world)
        {
            if (!world.IsSectorIdValid(MoveSpecial.SectorId))
                return null;

            return new StairSpecial(world, world.Sectors[MoveSpecial.SectorId], this);
        }
    }
}

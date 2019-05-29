using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class Sector
    {
        public readonly int Id;
        public readonly SectorFlat Floor;
        public readonly SectorFlat Ceiling;
        public readonly List<Side> Sides = new List<Side>();

        public Sector(int id, SectorFlat floor, SectorFlat ceiling)
        {
            Precondition(floor.Z <= ceiling.Z, "Sector floor is above the ceiling");

            Id = id;
            Floor = floor;
            Ceiling = ceiling;
        }

        public void Add(Side side)
        {
            Precondition(!Sides.Where(s => s.Id == side.Id).Any(), "Trying to add the same side twice");

            Sides.Add(side);
        }
    }
}

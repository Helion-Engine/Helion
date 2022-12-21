using Helion.World.Entities;
using System.Collections.Generic;

namespace Helion.World.Physics
{
    public class SectorMoveOrderComparer : IComparer<Entity>
    {
        public int Compare(Entity? x, Entity? y)
        {
            if (x == null || y == null)
                return 1;

            int compare = x.Position.Z.CompareTo(y.Position.Z);

            if (compare == 0)
                compare = x.Id.CompareTo(y.Id);

            return compare;
        }
    }
}

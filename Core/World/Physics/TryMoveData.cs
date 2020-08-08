using System.Collections.Generic;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    public class TryMoveData
    {
        public const double NoBlockingFloor = double.MinValue;

        public Vec2D Position;
        public bool Success;
        public double LowestCeilingZ;
        public double HighestFloorZ;
        public double HighestBlockingFloorZ = NoBlockingFloor;
        public double DropOffZ;

        public Entity? DropOffEntity;

        public List<Entity> IntersectEntities2D = new List<Entity>();

        public TryMoveData(Vec2D position)
        {
            Position = position;
        }

        public void SetIntersectionData(LineOpening opening)
        {
            if (opening.DropOffZ < DropOffZ)
            {
                DropOffZ = opening.DropOffZ;
                DropOffEntity = null;
            }
        }

        public void SetBlockingData(LineOpening opening)
        {
            if (opening.FloorZ > HighestBlockingFloorZ)
                HighestBlockingFloorZ = opening.FloorZ;
        }

        public void SetNonBlockingData(LineOpening opening)
        {
            if (opening.FloorZ > HighestFloorZ)
                HighestFloorZ = opening.FloorZ;
            if (opening.CeilingZ < LowestCeilingZ)
                LowestCeilingZ = opening.CeilingZ;
        }
    }
}

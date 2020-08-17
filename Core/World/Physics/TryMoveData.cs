using System.Collections.Generic;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

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
        public List<Line> IntersectSpecialLines = new List<Line>();

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

        public void AddIntersectSpecialLine(Line line)
        {
            if (!FindLine(IntersectSpecialLines, line.Id))
                IntersectSpecialLines.Add(line);
        }

        private static bool FindLine(List<Line> lines, int id)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Id == id)
                    return true;
            }

            return false;
        }
    }
}

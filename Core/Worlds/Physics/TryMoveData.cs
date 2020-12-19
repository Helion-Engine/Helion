using System.Collections.Generic;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Entities;
using Helion.Worlds.Geometry.Lines;

namespace Helion.Worlds.Physics
{
    public class TryMoveData
    {
        public Vec2D Position;
        public bool Success;
        public bool CanFloat;
        public double LowestCeilingZ;
        public double HighestFloorZ;
        public double DropOffZ;
        public Entity? DropOffEntity;
        public List<Entity> IntersectEntities2D = new();
        public List<Line> IntersectSpecialLines = new();

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

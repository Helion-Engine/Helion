using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics
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

        public List<Entity> IntersectEntities2D = new List<Entity>();
        public List<Line> IntersectSpecialLines = new List<Line>();
        public List<Line> ImpactSpecialLines = new List<Line>();

        public void SetPosition(in Vec2D position)
        {
            Position = position;
            CanFloat = false;
            IntersectEntities2D.Clear();
            IntersectSpecialLines.Clear();
            ImpactSpecialLines.Clear();
            DropOffEntity = null;
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

        public void AddImpactSpecialLine(Line line)
        {
            if (!FindLine(ImpactSpecialLines, line.Id))
                ImpactSpecialLines.Add(line);
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

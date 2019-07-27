using Helion.Maps.Geometry.Lines;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    public struct MoveInfo
    {
        public Entity? BlockingEntity;
        public Line? BlockingLine;
        public double LineIntersectionTime;
        public bool IntersectionFound;

        public MoveInfo(Entity? blockingEntity, Line? blockingLine, double lineIntersectionTime, bool foundHit)
        {
            BlockingEntity = blockingEntity;
            BlockingLine = blockingLine;
            LineIntersectionTime = lineIntersectionTime;
            IntersectionFound = foundHit;
        }

        public static MoveInfo Empty() => new MoveInfo(null, null, double.MaxValue, false);
    }
}
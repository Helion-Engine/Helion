using Helion.World.Geometry.Lines;

namespace Helion.World.Physics
{
    public readonly ref struct MoveInfo
    {
        public readonly Line BlockingLine;
        public readonly double LineIntersectionTime;
        public readonly bool IntersectionFound;

        public MoveInfo(Line blockingLine, double lineIntersectionTime, bool foundHit)
        {
            BlockingLine = blockingLine;
            LineIntersectionTime = lineIntersectionTime;
            IntersectionFound = foundHit;
        }

        public static MoveInfo Empty() => new MoveInfo(null!, double.MaxValue, false);
        
        public static MoveInfo From(Line line, double t) => new MoveInfo(line, t, true);
    }
}
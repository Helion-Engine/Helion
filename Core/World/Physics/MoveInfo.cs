namespace Helion.World.Physics;

public readonly ref struct MoveInfo(int blockLineIndex, double lineIntersectionTime, bool foundHit)
{
    public readonly int BlockLineIndex = blockLineIndex;
    public readonly double LineIntersectionTime = lineIntersectionTime;
    public readonly bool IntersectionFound = foundHit;

    public static MoveInfo Empty() => new(-1, double.MaxValue, false);

    public static MoveInfo From(int lineId, double t) => new(lineId, t, true);
}

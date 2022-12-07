namespace Helion.Geometry.New;

public record class Frustum2d(Seg2d Left, Seg2d Right, Seg2d Near, Seg2d Far)
{
    public bool InView(Vec2d point)
    {
        // TODO
        return false;
    }

    public bool InView(Seg2d seg)
    {
        // TODO
        return false;
    }

    public bool InView(Box2d box)
    {
        // TODO
        return false;
    }

    public bool InView(AABB2d aabb)
    {
        // TODO
        return false;
    }

    public bool TryClip(Seg2d seg, out Seg2d clippedSeg)
    {
        // TODO
        clippedSeg = default;
        return false;
    }
}

public record class Frustum3d(PlaneD Left, PlaneD Right, PlaneD Top, PlaneD Bottom, PlaneD Near, PlaneD Far)
{
    public bool InView(Vec3d point)
    {
        // TODO
        return false;
    }

    public bool InView(Seg3d seg)
    {
        // TODO
        return false;
    }

    public bool InView(Box3d box)
    {
        // TODO
        return false;
    }

    public bool InView(AABB3d aabb)
    {
        // TODO
        return false;
    }

    public bool TryClip(Seg3d seg, out Seg3d clippedSeg)
    {
        // TODO
        clippedSeg = default;
        return false;
    }
}

namespace Helion.Geometry.New;

public record class Frustum2(Seg2 Left, Seg2 Right, Seg2 Near, Seg2 Far)
{
    public bool InView(Vec2 point)
    {
        // TODO
        return false;
    }

    public bool InView(Seg2 seg)
    {
        // TODO
        return false;
    }

    public bool InView(Box2 box)
    {
        // TODO
        return false;
    }

    public bool TryClip(Seg2 seg, out Seg2 clippedSeg)
    {
        // TODO
        clippedSeg = default;
        return false;
    }
}

public record class Frustum3(Plane Left, Plane Right, Plane Top, Plane Bottom, Plane Near, Plane Far)
{
    public bool InView(Vec3 point)
    {
        // TODO
        return false;
    }

    public bool InView(Seg3 seg)
    {
        // TODO
        return false;
    }

    public bool InView(Box3 box)
    {
        // TODO
        return false;
    }

    public bool TryClip(Seg3 seg, out Seg3 clippedSeg)
    {
        // TODO
        clippedSeg = default;
        return false;
    }
}

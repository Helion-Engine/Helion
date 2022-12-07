namespace Helion.Geometry.New;

public readonly record struct Ray2d(Vec2d Pos, Vec2d Dir)
{
    public Seg2d ToSeg(double distance) => (Pos, Pos + (Dir * distance));
    public Vec2d FromTime(double t) => Pos + (Dir * t);
}

public readonly record struct Ray3d(Vec3d Pos, Vec3d Dir)
{
    public Seg3d ToSeg(double distance) => (Pos, Pos + (Dir * distance));
    public Vec3d FromTime(double t) => Pos + (Dir * t);
}

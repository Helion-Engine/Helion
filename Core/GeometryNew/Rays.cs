namespace Helion.GeometryNew;

public readonly record struct Ray3d(Vec3d Pos, Vec3d Dir)
{
    public Seg3d ToSeg(double distance) => (Pos, Pos + (Dir * distance));
    public Vec3d FromTime(double t) => Pos + (Dir * t);
}

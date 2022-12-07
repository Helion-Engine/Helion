namespace Helion.Geometry.New;

public readonly record struct Triangle<T>(T First, T Second, T Third);

public readonly record struct Triangle2f(Vec2f First, Vec2f Second, Vec2f Third)
{
    public double DoubleTriArea()
    {
        return ((First.X - Third.X) * (Second.Y - Third.Y)) - ((First.Y - Third.Y) * (Second.X - Third.X));
    }
}

public readonly record struct Triangle2d(Vec2d First, Vec2d Second, Vec2d Third)
{
    public double DoubleTriArea()
    {
        return ((First.X - Third.X) * (Second.Y - Third.Y)) - ((First.Y - Third.Y) * (Second.X - Third.X));
    }
}

public readonly record struct Triangle3f(Vec3f First, Vec3f Second, Vec3f Third);

public readonly record struct Triangle3d(Vec3d First, Vec3d Second, Vec3d Third);

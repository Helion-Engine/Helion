using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

public readonly record struct Triangle<T>(T First, T Second, T Third);

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Triangle2(Vec2 First, Vec2 Second, Vec2 Third)
{
    public float DoubleTriArea()
    {
        return ((First.X - Third.X) * (Second.Y - Third.Y)) - ((First.Y - Third.Y) * (Second.X - Third.X));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Triangle3(Vec3 First, Vec3 Second, Vec3 Third);

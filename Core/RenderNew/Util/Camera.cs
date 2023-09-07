using Helion.Geometry.Vectors;

namespace Helion.RenderNew.Util;

public readonly struct Camera(Vec3F Pos, Vec3F LookDir)
{
    public static readonly Vec3F Up = new(0, 0, 1);
    public static readonly Vec3F Right = new(1, 0, 0);
}
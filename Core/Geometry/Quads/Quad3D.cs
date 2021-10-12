using Helion.Geometry.Vectors;

namespace Helion.Geometry.Quads;

public readonly struct Quad3D
{
    public readonly Vec3D TopLeft;
    public readonly Vec3D TopRight;
    public readonly Vec3D BottomLeft;
    public readonly Vec3D BottomRight;

    public Quad3D(Vec3D topLeft, Vec3D topRight, Vec3D bottomLeft, Vec3D bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }
}

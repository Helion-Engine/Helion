using Helion.Geometry;
using Helion.Geometry.Vectors;

namespace Helion.Maps.Bsp.Builder.GLBSP;

public struct GLVertex
{
    public const int Bytes = 8;

    public readonly Fixed X;
    public readonly Fixed Y;

    public GLVertex(Fixed x, Fixed y)
    {
        X = x;
        Y = y;
    }

    public Vec2D ToDouble() => new(X.ToDouble(), Y.ToDouble());
}

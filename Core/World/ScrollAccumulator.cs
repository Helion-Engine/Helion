using Helion.Geometry.Vectors;

namespace Helion.World;

public struct ScrollAccumulator(Vec2D speed, Vec2I count)
{
    public static readonly ScrollAccumulator Zero = new(Vec2D.Zero, Vec2I.Zero);

    public Vec2D Speed = speed;
    public Vec2I Count = count;
}

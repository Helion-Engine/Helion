using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Maps.Bsp.Geometry;

public class BspVertex : Vector2D
{
    public readonly int Index;
    public readonly List<BspSegment> Edges = new();

    public Vec2D Position => new(X, Y);

    public BspVertex(Vec2D position, int index) : base(position.X, position.Y)
    {
        Index = index;
    }

    public override string ToString() => $"{base.ToString()} (index = {Index}, edgeCount = {Edges.Count})";
}

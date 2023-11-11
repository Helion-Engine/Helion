using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Lines;

namespace Helion.Render.Common.World.SelfReferencing;

public class SelfReferenceVertex
{
    public readonly Vec2D Pos;
    public readonly HashSet<Line> Lines;

    public bool HasOnlyTwoLines => Lines.Count == 2;
    
    public SelfReferenceVertex(Vec2D position, Line first)
    {
        Pos = position;
        Lines = new() { first };
    }

    public void Add(Line line)
    {
        Lines.Add(line);
    }

    public override bool Equals(object? obj)
    {
        return obj is SelfReferenceVertex other && Pos == other.Pos;
    }

    public override int GetHashCode()
    {
        return Pos.GetHashCode();
    }
}
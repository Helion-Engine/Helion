using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.GeometryNew;

public class Polygon2D : IEnumerable<Vec2d>
{
    public readonly Vec2d[] Vertices;

    public int Length => Vertices.Length;

    public Polygon2D(Vec2d[] vertices)
    {
        Debug.Assert(vertices.Length >= 3, "Polygon requires at least 3 vertices to not be malformed");

        Vertices = vertices;
    }

    public Vec2d this[int index] => Vertices[index];

    public IEnumerator<Vec2d> GetEnumerator() => ((IEnumerable<Vec2d>)Vertices).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Vertices.GetEnumerator();
}

public class ConvexPolygon2D : Polygon2D
{
    public ConvexPolygon2D(Vec2d[] vertices) : base(vertices)
    {
    }

    public IEnumerable<Triangle2D> GetTriangles()
    {
        for (int i = 2; i < Vertices.Length; i++)
            yield return new(Vertices[i - 2], Vertices[i - 1], Vertices[i]);
    }
}

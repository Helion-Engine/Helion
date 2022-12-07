using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Geometry.New;

public class Polygon2<T> : IEnumerable<T>
{
    public readonly T[] Vertices;

    public int Length => Vertices.Length;

    public Polygon2(params T[] vertices)
    {
        Debug.Assert(vertices.Length >= 3, "Polygon requires at least 3 vertices to not be malformed");

        Vertices = vertices;
    }

    public Polygon2(ReadOnlySpan<T> vertices) : this(vertices.ToArray())
    {
    }

    public T this[int index] => Vertices[index];

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Vertices).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Vertices.GetEnumerator();

}

public class ConvexPolygon2<T> : Polygon2<T>
{
}

public class ConvexPolygon2D : ConvexPolygon2<Vec2d>
{
}

public static class ConvexPolygonExtensions
{
    public static IEnumerable<Triangle2<TVec>> GetTriangleFan<TVec>(this ConvexPolygon2<TVec> polygon)
        where TVec : struct
    {
        for (int i = 2; i < polygon.Vertices.Length; i++)
            yield return new(polygon.Vertices[i - 2], polygon.Vertices[i - 1], polygon.Vertices[0]);
    }

    public static bool TryClip<TVec>(this ConvexPolygon2<TVec> polygon, [NotNullWhen(true)] out TVec[]? clippedVertices)
        where TVec : struct
    {
        // TODO

        clippedVertices = null;
        return false;
    }
}

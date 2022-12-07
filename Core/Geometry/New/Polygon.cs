using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Helion.Geometry.New;

public class Polygon<T> : IEnumerable<T>
{
    public readonly T[] Vertices;

    public int Length => Vertices.Length;

    public Polygon(params T[] vertices)
    {
        Debug.Assert(vertices.Length >= 3, "Polygon requires at least 3 vertices to not be malformed");

        Vertices = vertices;
    }

    public Polygon(ReadOnlySpan<T> vertices) : this(vertices.ToArray())
    {
    }

    public T this[int index] => Vertices[index];

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Vertices).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Vertices.GetEnumerator();
}

// This is meant to imply to the user that they can assume it is convex.
public class ConvexPolygon<T> : Polygon<T>
{
}

public class ConvexPolygon2 : ConvexPolygon<Vec2>
{
    // If true, a set of clipped vertices with a non-zero area was returned. Otherwise,
    // false means it was completely clipped, or clipped down to a single point or line.
    public bool TryClip(Box2 box, out List<Vec2> clippedVertices)
    {
        // This is the Sutherland-Hodgman algorithm.
        //
        // It needs to have all of the edges done such that the right side always points
        // inside the box, or else the algorithm will not work.
        Span<Seg2> clipEdges = stackalloc Seg2[4];
        clipEdges[0] = ((box.Min.X, box.Min.Y), (box.Min.X, box.Max.Y));
        clipEdges[1] = ((box.Min.X, box.Max.Y), (box.Max.X, box.Max.Y));
        clipEdges[2] = ((box.Max.X, box.Max.Y), (box.Max.X, box.Min.Y));
        clipEdges[3] = ((box.Max.X, box.Min.Y), (box.Min.X, box.Min.Y));

        List<Vec2> outputList = Vertices.ToList();
        
        foreach (Seg2 clipEdge in clipEdges)
        {
            List<Vec2> inputList = outputList.ToList();
            outputList.Clear();

            for (int i = 0; i < inputList.Count; i++)
            {
                int prevIndex = (i - 1 + inputList.Count) % inputList.Count;
                Vec2 prevPoint = inputList[prevIndex];
                Vec2 currPoint = inputList[i];

                bool currInside = clipEdge.OnRight(currPoint);
                bool prevInside = clipEdge.OnRight(prevPoint);
                if (currInside)
                {
                    Seg2 polygonEdge = (prevPoint, currPoint);
                    polygonEdge.TryIntersect(clipEdge, out Vec2 intersectPoint);
                    if (!prevInside)
                        outputList.Add(intersectPoint);
                    outputList.Add(currPoint);
                }
                else if (prevInside)
                {
                    Seg2 polygonEdge = (prevPoint, currPoint);
                    polygonEdge.TryIntersect(clipEdge, out Vec2 intersectPoint);
                    outputList.Add(intersectPoint);
                }
            }
        }

        clippedVertices = outputList;
        return clippedVertices.Count >= 3;
    }
}

public static class ConvexPolygonExtensions
{
    // The fan is always in an order of [(0, 1, 2), (0, 2, 3), ..., (0, n-2, n-1)].
    public static IEnumerable<(TVec, TVec, TVec)> GetTriangleFan<TVec>(this ConvexPolygon<TVec> polygon)
    {
        for (int i = 2; i < polygon.Vertices.Length; i++)
            yield return (polygon.Vertices[0], polygon.Vertices[i - 1], polygon.Vertices[i]);
    }
}

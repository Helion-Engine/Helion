using System;
using System.Collections.Generic;
using System.Linq;
using Helion.GeometryNew.Segments;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Boxes;

public struct Box2
{
    public Vec2 Min;
    public Vec2 Max;
    
    public float Top => Max.Y;
    public float Bottom => Min.Y;
    public float Left => Min.X;
    public float Right => Max.X;
    public Vec2 TopLeft => (Left, Top);
    public Vec2 BottomLeft => Min;
    public Vec2 BottomRight => (Right, Bottom);
    public Vec2 TopRight => Max;
    public float Width => Right - Left;
    public float Height => Top - Bottom;
    public Vec2 Sides => Max - Min;
    public Vec2 Extent => Sides * 0.5f;
    public Vec2 Center => Min + Extent;

    public Box2(Vec2 min, Vec2 max)
    {
        Min = min;
        Max = max;
    }
    
    public static implicit operator Box2(ValueTuple<float, float, float, float> tuple)
    {
        return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
    }

    public static implicit operator Box2(ValueTuple<Vec2, Vec2> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }
    
    public static Box2 operator +(Box2 self, Vec2 offset) => (self.Min + offset, self.Max + offset);
    public static Box2 operator -(Box2 self, Vec2 offset) => (self.Min - offset, self.Max - offset);
    public static Box2 operator *(Box2 self, float scale) => (self.Min * scale, self.Max * scale);
    public static Box2 operator /(Box2 self, float divisor) => (self.Min / divisor, self.Max / divisor);
    
    public Box2 Bound(Box2 box) => (Min.Min(box.Min), Max.Max(box.Max));
    public Vec2 Clamp(Vec2 point) => (point.X.Clamp(Left, Right), point.Y.Clamp(Bottom, Top));
    public bool Contains(Vec2 point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3 point) => Contains(point.XY);
    public bool Intersects(Box2 box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
 
    /// <summary>
    /// Clips the polygon to this box.
    /// </summary>
    /// <param name="convexVertices">The vertices of the convex polygon.</param>
    /// <returns>The clipped list, or an empty list if all edges were clipped.
    public List<Vec2> Clip(ReadOnlySpan<Vec2> convexVertices)
    {
        // The most we can bring into existence would be 4 extra vertices
        // for the 4 edges, due to both being convex shapes.
        int maxPossibleVertices = convexVertices.Length + 4;
        
        // Source: https://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
        // TODO: I bet this can be optimized a lot to use provided spans/stack.
        List<Vec2> outputList = new(maxPossibleVertices);
        
        // These need to make the right side be facing inside, so go clockwise.
        ReadOnlySpan<Seg2> clipPolygon = stackalloc Seg2[4]
        {
            (BottomLeft, TopLeft), 
            (TopLeft, TopRight), 
            (TopRight, BottomRight), 
            (BottomRight, BottomLeft)
        };

        for (int edgeIdx = 0; edgeIdx < clipPolygon.Length; edgeIdx++)
        {
            Seg2 clipEdge = clipPolygon[edgeIdx];
            
            List<Vec2> inputList = outputList.ToList();
            outputList.Clear();

            for (int inputIdx = 0; inputIdx < inputList.Count; inputIdx++)
            {
                Vec2 currentPoint = inputList[inputIdx];
                int prevIdx = inputIdx > 0 ? inputIdx : inputList.Count - 1;
                Vec2 prevPoint = inputList[prevIdx];
                Seg2 polygonEdge = (prevPoint, currentPoint);

                if (polygonEdge.LineIntersection(clipEdge, out Vec2 intersectPoint))
                {
                    if (currentPoint.OnRight(clipEdge))
                    {
                        if (!prevPoint.OnRight(clipEdge))
                            outputList.Add(intersectPoint);
                        outputList.Add(currentPoint);
                    }
                    else if (prevPoint.OnRight(clipEdge))
                    {
                        outputList.Add(intersectPoint);
                    }
                }
            }
        }
        
        return outputList;
    }
    
    public override string ToString() => $"({Min}), ({Max})";
}
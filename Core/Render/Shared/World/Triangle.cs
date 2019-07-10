using System.Numerics;
using Helion.Maps.Geometry;
using Helion.Util;

namespace Helion.Render.Shared.World
{
    /// <summary>
    /// A vertex in a world which holds positional and UV texture information.
    /// </summary>
    public struct Vertex
    {
        public Vector3 Position;
        public Vector2 UV;

        public Vertex(Vector3 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }
    }
    
    /// <summary>
    /// A simple triangle that is the result of a triangulation of some world
    /// component. The vertices are to be in counter-clockwise order.
    /// </summary>
    public struct Triangle
    {
        public Vertex First;
        public Vertex Second;
        public Vertex Third;

        public Triangle(Vertex first, Vertex second, Vertex third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
    
    /// <summary>
    /// A quadrilateral that makes up some wall component, which is the
    /// indivisible unit of a side.
    /// </summary>
    /// <remarks>
    /// The hierarchy is: Lines have two sides, sides have 1-3 or more walls or
    /// more if there's 3D floors involved.
    /// </remarks>
    public class WallQuad
    {
        public readonly Vertex TopLeft;
        public readonly Vertex TopRight;
        public readonly Vertex BottomLeft;
        public readonly Vertex BottomRight;
        public readonly UpperString Texture;
        public readonly Side Side;
        public readonly SectorFlat Floor;
        public readonly SectorFlat Ceiling;

        public WallQuad(Vertex topLeft, Vertex topRight, Vertex bottomLeft, Vertex bottomRight, 
            UpperString texture, Side side, SectorFlat floor, SectorFlat ceiling)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            Texture = texture;
            Side = side;
            Floor = floor;
            Ceiling = ceiling;
        }
    }
}
using System.Collections.Generic;
using System.Numerics;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;

namespace Helion.Render.Shared.World
{
    public struct PositionQuad
    {
        public readonly Vector3 TopLeft;
        public readonly Vector3 TopRight;
        public readonly Vector3 BottomLeft;
        public readonly Vector3 BottomRight;

        public PositionQuad(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }
    }
    
    public struct UVQuad
    {
        public readonly Vector2 TopLeft;
        public readonly Vector2 TopRight;
        public readonly Vector2 BottomLeft;
        public readonly Vector2 BottomRight;

        public UVQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
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

        /// <summary>
        /// Creates a degenerate quad, so nothing will be rendered by the
        /// renderer. This ends up being a point at the origin.
        /// </summary>
        /// <param name="side">The side for the quad.</param>
        /// <param name="floor">The floor for this quad.</param>
        /// <param name="ceiling">The ceiling for this quad.</param>
        /// <returns>A degenerate quad, which is a point.</returns>
        public static WallQuad Degenerate(Side side, SectorFlat floor, SectorFlat ceiling)
        {
            Vertex origin = new Vertex(Vector3.Zero, Vector2.Zero);
            return new WallQuad(origin, origin,origin,origin, 
                                Constants.NoTexture, side, floor, ceiling);
        }
    }

    public class SideTriangles
    {
        public readonly List<WallQuad> Walls;
        private readonly Side m_side;

        public WallQuad Middle => Walls[0];
        public WallQuad? Upper => m_side.Line.TwoSided ? Walls[1] : null;
        public WallQuad? Lower => m_side.Line.TwoSided ? Walls[2] : null;

        public SideTriangles(Side side, WallQuad middle)
        {
            Walls = new List<WallQuad>(){middle};
            m_side = side;
        }
        
        public SideTriangles(Side side, WallQuad middle, WallQuad upper, WallQuad lower)
        {
            Walls = new List<WallQuad>(){middle, upper, lower};
            m_side = side;
        }
    }
    
    public class LineTriangles
    {
        public readonly SideTriangles Front;
        public readonly SideTriangles? Back;

        public SideTriangles[] SideTriangles => Back != null ? new[] {Front, Back} : new[] {Front};

        public LineTriangles(Line line, SideTriangles front, SideTriangles? back = null)
        {
            Front = front;
            Back = back;
        }
    }
}
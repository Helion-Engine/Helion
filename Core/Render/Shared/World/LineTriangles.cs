using System.Collections.Generic;
using System.Numerics;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;

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
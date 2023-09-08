using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.RenderNew.Interfaces.World;

namespace Helion.RenderNew.Renderers.World.Geometry;

// These numbers are necessary so it can be casted to bits.
public enum WallVertexCorner
{
    TopLeft = 0b00, 
    TopRight = 0b01,
    BottomLeft = 0b10,
    BottomRight = 0b11
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct WallVertex
{
    public Vec2F Pos;
    public int SideIdx;
    public int Flags; // 31:4 = unused, 3:2 = section, 1:0 = corner

    public WallVertex(Vec2F pos, int sideIdx, WallSection section, WallVertexCorner corner)
    {
        Pos = pos;
        SideIdx = sideIdx;
        Flags = ((int)section << 2) | (int)corner;
    }
}

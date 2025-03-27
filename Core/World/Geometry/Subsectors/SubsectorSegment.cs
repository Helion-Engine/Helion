using Helion.Geometry.Vectors;
using System.Runtime.InteropServices;

namespace Helion.World.Geometry.Subsectors;

/// <summary>
/// An edge of a subsector.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct SubsectorSegment
{
    public readonly Vec2D Start;
    public readonly Vec2D End;

    public readonly int SideId;
    public readonly int LineId;

    public SubsectorSegment(int sideId, int lineId, Vec2D start, Vec2D end)
    {
        SideId = sideId;
        LineId = lineId;
        Start = start;
        End = end;
    }
}

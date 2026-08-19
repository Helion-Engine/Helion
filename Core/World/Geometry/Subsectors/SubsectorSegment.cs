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

    public readonly int FrontSectorId;
    public readonly int BackSectorId;

    public SubsectorSegment(int sideId, int lineId, Vec2D start, Vec2D end, int frontSectorId, int backSectorId)
    {
        SideId = sideId;
        LineId = lineId;
        Start = start;
        End = end;
        FrontSectorId = frontSectorId;
        BackSectorId = backSectorId;
    }
}

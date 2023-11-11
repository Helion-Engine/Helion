using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sides;
using System.Runtime.InteropServices;

namespace Helion.World.Geometry.Subsectors;

[StructLayout(LayoutKind.Sequential)]
public readonly struct SubsectorSegment
{
    public readonly Vec2D Start;
    public readonly Vec2D End;
    public readonly Side? Side;
    public readonly int? PartnerSegId;
    public readonly int SubsectorId;

    public bool IsMiniseg => Side == null;

    public SubsectorSegment(Side? side, Vec2D start, Vec2D end, int? partnerSegId, int subsectorId)
    {
        Side = side;
        Start = start;
        End = end;
        PartnerSegId = partnerSegId;
        SubsectorId = subsectorId;
    }
}

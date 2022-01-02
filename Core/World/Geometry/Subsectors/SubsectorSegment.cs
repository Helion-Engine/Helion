using Helion.Geometry.Vectors;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
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

    /// <summary>
    /// The side this belongs to, if any (will be null if it's a miniseg).
    /// </summary>
    public readonly Side? Side;

    /// <summary>
    /// Gets the line that this segment is on, or null if it's a miniseg.
    /// </summary>
    //public Line? Line => Side?.Line;

    /// <summary>
    /// Checks if this is a miniseg or not (is along the empty splitter and
    /// does not map onto any line/side directly).
    /// </summary>
    public bool IsMiniseg => Side == null;

    /// <summary>
    /// Creates a new subsector segment.
    /// </summary>
    /// <param name="side">The side this belongs to, or null if this is a
    /// miniseg.</param>
    /// <param name="start">The starting point of this segment.</param>
    /// <param name="end">The ending point of this segment.</param>
    public SubsectorSegment(Side? side, Vec2D start, Vec2D end)
    {
        Side = side;
        Start = start;
        End = end;
    }
}

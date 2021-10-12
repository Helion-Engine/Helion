using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Subsectors;

/// <summary>
/// An edge of a subsector.
/// </summary>
public class SubsectorSegment : Segment2D
{
    /// <summary>
    /// The unique ID for the segment.
    /// </summary>
    public readonly int Id;

    /// <summary>
    /// The subsector for this segment.
    /// </summary>
    public Subsector Subsector { get; internal set; }

    /// <summary>
    /// The side this belongs to, if any (will be null if it's a miniseg).
    /// </summary>
    public Side? Side;

    /// <summary>
    /// Gets the line that this segment is on, or null if it's a miniseg.
    /// </summary>
    public Line? Line => Side?.Line;

    /// <summary>
    /// Checks if this is a miniseg or not (is along the empty splitter and
    /// does not map onto any line/side directly).
    /// </summary>
    public bool IsMiniseg => Side == null;

    /// <summary>
    /// Creates a new subsector segment.
    /// </summary>
    /// <param name="id">The unique ID of the segment.</param>
    /// <param name="side">The side this belongs to, or null if this is a
    /// miniseg.</param>
    /// <param name="start">The starting point of this segment.</param>
    /// <param name="end">The ending point of this segment.</param>
    public SubsectorSegment(int id, Side? side, Vec2D start, Vec2D end) : base(start, end)
    {
        Id = id;
        Side = side;

        // As seen elsewhere: if this is not set shortly thereafter,
        // someone screwed up big time. This also avoids making a very
        // messy workaround to make this correct.
        Subsector = null !;
    }
}


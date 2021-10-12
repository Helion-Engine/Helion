using Helion.Geometry.Vectors;
using Helion.Maps.Components;

namespace Helion.Bsp.Geometry;

/// <summary>
/// Indicates a line can be used for BSP actions within the BSP builder.
/// </summary>
public interface IBspUsableLine
{
    /// <summary>
    /// A unique identifier for this line.
    /// </summary>
    /// <remarks>
    /// This can also mean (in the case of <see cref="ILine"/> extending
    /// from this) the ID of the line in the LINEDEFS entry.
    /// </remarks>
    int Id { get; }

    /// <summary>
    /// The starting vertex of the line.
    /// </summary>
    Vec2D StartPosition { get; }

    /// <summary>
    /// The ending vertex of the line.
    /// </summary>
    Vec2D EndPosition { get; }

    /// <summary>
    /// If the line is one-sided or not.
    /// </summary>
    bool OneSided { get; }
}


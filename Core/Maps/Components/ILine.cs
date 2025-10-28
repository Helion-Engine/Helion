using Helion.Maps.Bsp.Geometry;
using Helion.Maps.Shared;

namespace Helion.Maps.Components;

/// <summary>
/// A line in a map.
/// </summary>
public interface ILine : IBspUsableLine
{
    /// <summary>
    /// The flags for the line.
    /// </summary>
    MapLineFlags Flags { get; }

    /// <summary>
    /// Gets the front side of the line.
    /// </summary>
    /// <returns>The front side of the line.</returns>
    ISide GetFront();

    /// <summary>
    /// Gets the back side of the line.
    /// </summary>
    /// <returns>The back side of the line.</returns>
    ISide? GetBack();

    int Special { get; }
    int SectorTag { get; }
}

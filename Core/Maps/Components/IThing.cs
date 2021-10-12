using Helion.Geometry.Vectors;
using Helion.Maps.Shared;

namespace Helion.Maps.Components;

/// <summary>
/// Represents an entity in a map.
/// </summary>
public interface IThing
{
    /// <summary>
    /// The ID of the thing. This is unique for every single thing.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// The thing ID for the thing. This is different from the ID since
    /// this ID is for looking up things in the map and may be used on
    /// multiple things, meaning it is not unique.
    /// </summary>
    int ThingId { get; }

    /// <summary>
    /// The position in the map. The Z coordinate will be the most negative
    /// number in implementations that do not support the third dimension.
    /// </summary>
    Vec3Fixed Position { get; }

    /// <summary>
    /// The angle (in degrees).
    /// </summary>
    ushort Angle { get; }

    /// <summary>
    /// The editor number for what type this is.
    /// </summary>
    ushort EditorNumber { get; }

    /// <summary>
    /// The flags for the thing.
    /// </summary>
    ThingFlags Flags { get; }
}

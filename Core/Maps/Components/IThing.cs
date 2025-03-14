using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Components;

/// <summary>
/// Represents an entity in a map.
/// </summary>
public interface IThing
{
    int Id { get; }
    int ThingId { get; }
    Vec3D Position { get; }
    ushort Angle { get; }
    ushort EditorNumber { get; }
    ThingFlags Flags { get; }
    ZDoomLineSpecialType Special { get; }
    ref SpecialArgs Args { get; }
    float? Alpha { get; }
    float Gravity { get; }
    int? Health { get; }
}

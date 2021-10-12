using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;

namespace Helion.Maps.Doom.Components;

public class DoomThing : IThing
{
    public int Id { get; }
    public int ThingId { get; } = 0;
    public Vec3Fixed Position { get; }
    public ushort Angle { get; }
    public ushort EditorNumber { get; }
    public ThingFlags Flags { get; }

    internal DoomThing(int id, Vec2Fixed position, ushort angle, ushort editorNumber, ThingFlags flags)
    {
        Id = id;
        Position = new Vec3Fixed(position.X, position.Y, Fixed.Lowest());
        Angle = angle;
        EditorNumber = editorNumber;
        Flags = flags;
    }
}


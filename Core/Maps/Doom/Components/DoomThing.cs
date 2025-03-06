using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Specials;

namespace Helion.Maps.Doom.Components;

public class DoomThing : IThing
{
    public int Id { get; }
    public int ThingId { get; } = 0;
    public Vec3D Position { get; }
    public ushort Angle { get; }
    public ushort EditorNumber { get; }
    public ThingFlags Flags { get; }
    public ZDoomLineSpecialType Special { get; set; }
    public ref SpecialArgs Args => ref _args;

    private SpecialArgs _args;

    internal DoomThing(int id, Vec2Fixed position, ushort angle, ushort editorNumber, ThingFlags flags)
    {
        Id = id;
        Position = new Vec3D(position.X.ToDouble(), position.Y.ToDouble(), double.MinValue);
        Angle = angle;
        EditorNumber = editorNumber;
        Flags = flags;
    }
}

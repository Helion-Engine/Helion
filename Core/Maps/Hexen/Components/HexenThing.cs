using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Hexen.Components;

public class HexenThing : IThing
{
    public int Id { get; }
    public int ThingId { get; }
    public Vec3D Position { get; }
    public ushort Angle { get; }
    public ushort EditorNumber { get; }
    public ThingFlags Flags { get; }
    public ZDoomLineSpecialType Special { get; set; }
    public ref SpecialArgs Args => ref _args;

    private SpecialArgs _args;
    public float? Alpha => null;
    public float Gravity => 1f;
    public int? Health => null;

    internal HexenThing(int id, ushort tid, Vec3D position, ushort angle, ushort editorNumber,
        ThingFlags flags, ZDoomLineSpecialType special, SpecialArgs args)
    {
        Id = id;
        ThingId = tid;
        Position = position;
        Angle = angle;
        EditorNumber = editorNumber;
        Flags = flags;
        Special = special;
        Args = args;
    }
}

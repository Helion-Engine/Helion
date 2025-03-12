using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Udmf.Components;

public class UdmfThing : IThing
{
    public int Id { get; set; }

    public int ThingId { get; set; }

    public Vec3D Position { get; set; }

    public ushort Angle { get; set; }

    public ushort EditorNumber { get; set; }

    public ThingFlags Flags { get; set; } = new();
    public ZDoomLineSpecialType Special { get; set; }
    public ref SpecialArgs Args => ref _args;

    private SpecialArgs _args;
    public float Alpha { get; set; } = 1f;
    public float Gravity { get; set; } = 1f;
}

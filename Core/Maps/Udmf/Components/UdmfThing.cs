using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;

namespace Helion.Maps.Udmf.Components;

public class UdmfThing : IThing
{
    public int Id { get; set; }

    public int ThingId { get; set; }

    public Vec3D Position { get; set; }

    public ushort Angle { get; set; }

    public ushort EditorNumber { get; set; }

    public ThingFlags Flags { get; set; } = new();
    public SpecialArgs Args;
}

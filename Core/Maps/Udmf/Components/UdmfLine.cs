using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Special;

namespace Helion.Maps.Udmf.Components;

public class UdmfLine : ILine
{
    public MapLineFlags Flags { get; set; } = MapLineFlags.ZDoom(0);
    public int Id { get; set; }
    public Vec2D StartPosition { get; set; }
    public Vec2D EndPosition { get; set; }
    public bool OneSided { get; set; }

    public UdmfSide Front { get; set; } = null!;
    public UdmfSide? Back { get; set; }
    public ZDoomLineSpecialType Special;
    public LineActivationType ActivationType = LineActivationType.Any;
    public SpecialArgs Args;
    public float Alpha = 1f;
    public int StartVertex;
    public int EndVertex;
    public int SideFront;
    public int? SideBack;

    public ISide GetFront() => Front;

    public ISide? GetBack() => Back;
}

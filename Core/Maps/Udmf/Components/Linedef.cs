using Helion.Maps.Shared;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Specials;
using Helion.World.Special;

namespace Helion.Maps.Udmf.Components;

internal class Linedef
{
    public int StartVertex;
    public int EndVertex;
    public int SideFront;
    public int? SideBack;
    public ZDoomLineSpecialType Special;
    public MapLineFlags Flags = MapLineFlags.ZDoom(0);
    public LineActivationType ActivationType = LineActivationType.Any;
    public SpecialArgs Args;
    public float Alpha = 1f;
}

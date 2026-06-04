using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;

namespace Helion.Models;

public struct LineModel
{
    public int Id;
    public int DataChanges;
    public SideModel? Front;
    public SideModel? Back;
    public SpecialArgs? Args;
    public LineBlockFlags? BlockFlags;
    public float? Alpha;
    public bool BlockSound;
    public ZDoomLineSpecialType Special;
}

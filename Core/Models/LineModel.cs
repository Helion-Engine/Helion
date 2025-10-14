using Helion.Maps.Specials;

namespace Helion.Models;

public struct LineModel
{
    public int Id;
    public int DataChanges;
    public SideModel? Front;
    public SideModel? Back;
    public SpecialArgs? Args;
    public float? Alpha;
}

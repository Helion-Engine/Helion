using Helion.Maps.Specials;

namespace Helion.Models;

public struct LineModel
{
    public int Id { get; set; }
    public int DataChanges { get; set; }
    public SideModel? Front { get; set; }
    public SideModel? Back { get; set; }
    public SpecialArgs? Args { get; set; }
    public float? Alpha { get; set; }
}

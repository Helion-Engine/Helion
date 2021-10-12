using Helion.Maps.Specials;

namespace Helion.Models;

public class LineModel
{
    public int Id { get; set; }
    public int DataChanges { get; set; }
    public bool? Activated { get; set; }
    public SideModel? Front { get; set; }
    public SideModel? Back { get; set; }
    public SpecialArgs? Args { get; set; }
    public float? Alpha { get; set; }
}


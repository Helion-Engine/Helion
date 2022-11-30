namespace Helion.Models;

public class SideModel
{
    public int DataChanges { get; set; }
    // Integer texture handles are deprecated here. Keeping for backwards compatibiity.
    public int? UpperTexture { get; set; }
    public int? MiddleTexture { get; set; }
    public int? LowerTexture { get; set; }
    public string? UpperTex { get; set; }
    public string? MiddelTex { get; set; }
    public string? LowerTex { get; set; }
    public double[]? FrontOffsetX { get; set; }
    public double[]? FrontOffsetY { get; set; }
    public double[]? BackOffsetX { get; set; }
    public double[]? BackOffsetY { get; set; }
}

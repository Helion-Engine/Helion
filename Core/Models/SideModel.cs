namespace Helion.Models;

public struct SideModel
{
    public int DataChanges;
    // Integer texture handles are deprecated here. Keeping for backwards compatibiity.
    public int? UpperTexture;
    public int? MiddleTexture;
    public int? LowerTexture;
    public string? UpperTex;
    public string? MiddelTex;
    public string? LowerTex;
}

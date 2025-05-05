namespace Helion.Resources.Definitions.Zdoom;

public class BrightmapDefinition
{
    public string TargetTexture { get; set; } = "";
    // some brightmaps packs will not have a brightmap, but will disable the fullbright
    public string? BrightmapName { get; set; }
    public bool IwadOnly { get; set; }
    /// <summary>Used for `thiswad` option</summary>
    public string? SpecificWadMd5 { get; set; }
    public bool DisableFullbright { get; set; }
}
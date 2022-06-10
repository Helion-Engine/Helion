namespace Helion.Resources.Definitions.Animdefs;

public class AnimatedRange : IAnimatedRange
{
    public string StartTexture { get; set; } = string.Empty;
    public string EndTexture { get; set; } = string.Empty;
    public int StartTextureIndex { get; set; } = -1;
    public int EndTextureIndex { get; set; } = -1;
    public int MinTics { get; set; }
    public int MaxTics { get; set; }
    public ResourceNamespace Namespace { get; set; }
    public bool Optional { get; set; }
    public bool Oscillate { get; set; }
}

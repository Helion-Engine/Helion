using Helion.Resources.Definitions.Animdefs;

namespace Helion.Resources.Definitions.Boom;

public class BoomAnimatedTexture : IAnimatedRange
{
    public string StartTexture { get; set; } = string.Empty;
    public string EndTexture { get; set; } = string.Empty;
    public int MinTics { get; set; }
    public int MaxTics { get; set; }
    public bool IsTexture { get; set; }
    public int StartTextureIndex => -1;
    public int EndTextureIndex => -1;
    public ResourceNamespace Namespace => IsTexture ? ResourceNamespace.Textures : ResourceNamespace.Flats;
    public bool Optional => false;
    public bool Oscillate => false;
}

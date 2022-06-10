namespace Helion.Resources.Definitions.Animdefs;

internal interface IAnimatedRange
{
    string StartTexture { get; }
    string EndTexture { get; }
    int StartTextureIndex { get; }
    int EndTextureIndex { get; }
    int MinTics { get; }
    int MaxTics { get; }
    ResourceNamespace Namespace { get; }
    bool Optional { get; }
    bool Oscillate { get; }
}

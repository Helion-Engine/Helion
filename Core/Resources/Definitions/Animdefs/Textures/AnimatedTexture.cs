using System.Collections.Generic;

namespace Helion.Resources.Definitions.Animdefs.Textures;

public sealed class AnimatedTexture
{
    public string Name;
    public bool Optional;
    public ResourceNamespace Namespace;
    public IList<AnimatedTextureComponent> Components = new List<AnimatedTextureComponent>();
    public bool AllowDecals;
    public bool Oscillate;
    public bool Random;

    public AnimatedTexture(string name, bool optional, ResourceNamespace resourceNamespace)
    {
        Name = name;
        Optional = optional;
        Namespace = resourceNamespace;
    }

    public override string ToString() => $"{Name} (len={Components.Count})";
}

using System.Collections.Generic;

namespace Helion.Resources.Definitions.Animdefs.Textures;

public class AnimatedTexture
{
    public readonly string Name;
    public readonly bool Optional;
    public readonly ResourceNamespace Namespace;
    public readonly IList<AnimatedTextureComponent> Components = new List<AnimatedTextureComponent>();
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

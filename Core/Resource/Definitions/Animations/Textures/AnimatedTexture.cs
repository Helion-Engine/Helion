using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Textures
{
    public class AnimatedTexture : IAnimatedTexture
    {
        public CIString Name { get; }
        public Namespace Namespace { get; }
        public readonly bool Optional;
        public readonly List<AnimatedTextureComponent> Components = new();
        public bool AllowDecals;
        public bool Oscillate;
        public bool Random;

        public AnimatedTexture(string name, bool optional, Namespace resourceNamespace)
        {
            Name = name.ToUpper();
            Optional = optional;
            Namespace = resourceNamespace;
        }

        public override string ToString() => $"{Name} (len={Components.Count})";
    }
}
using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resources.Definitions.Animdefs.Textures
{
    public class AnimatedTexture
    {
        public readonly CIString Name;
        public readonly bool Optional;
        public bool AllowDecals;
        public bool Oscillate;
        public bool Random;
        public IList<AnimatedTextureComponent> Components = new List<AnimatedTextureComponent>();

        public AnimatedTexture(string name, bool optional)
        {
            Name = name.ToUpper();
            Optional = optional;
        }
    }
}
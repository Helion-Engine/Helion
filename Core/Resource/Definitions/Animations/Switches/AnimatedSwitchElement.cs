using System.Collections.Generic;
using Helion.Resource.Definitions.Animations.Textures;
using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Switches
{
    public class AnimatedSwitchElement
    {
        public CIString? Sound;
        public readonly List<AnimatedTextureComponent> Components = new();
    }
}

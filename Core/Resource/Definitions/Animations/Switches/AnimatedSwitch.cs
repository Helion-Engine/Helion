using System.Collections.Generic;
using Helion.Resource.Definitions.Animations.Textures;
using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Switches
{
    public class AnimatedSwitch : IAnimatedTexture
    {
        public CIString Name => StartTexture;
        public Namespace Namespace => Namespace.Textures;
        public readonly CIString StartTexture;
        public readonly SwitchType SwitchType;
        public readonly List<AnimatedTextureComponent> Components = new();
        public CIString? Sound;

        public AnimatedSwitch(CIString texture, SwitchType switchType)
        {
            StartTexture = texture;
            SwitchType = switchType;
        }

        public override string ToString() => $"{StartTexture} ({SwitchType}: components={Components.Count})";
    }
}
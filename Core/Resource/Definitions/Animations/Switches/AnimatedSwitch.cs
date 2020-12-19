using Helion.Resource.Definitions.Animations.Textures;
using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Switches
{
    public class AnimatedSwitch : IAnimatedTexture
    {
        public CIString Name { get; }
        public Namespace Namespace => Namespace.Textures;
        public readonly AnimatedSwitchElement On = new();
        public readonly AnimatedSwitchElement Off = new();

        public AnimatedSwitch(CIString name)
        {
            Name = name;
        }

        public override string ToString() => $"{Name} (On count: {On.Components.Count}, Off count: {Off.Components.Count})";
    }
}
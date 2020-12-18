using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resource.Definitions.Animations.Textures
{
    public class AnimatedTextureComponent
    {
        public readonly CIString Texture;
        public readonly int MinTicks;
        public readonly int MaxTicks;

        public AnimatedTextureComponent(CIString texture, int min, int max)
        {
            Precondition(!texture.Empty(), "Cannot have an empty texture component name");
            Precondition(min >= 0 && min <= max, "Min must be positive and max must not be less than min");

            Texture = texture;
            MinTicks = min;
            MaxTicks = max;
        }

        public override string ToString() => $"{Texture} (startTicks={MinTicks} endTicks={MaxTicks})";
    }
}
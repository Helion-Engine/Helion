using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Animdefs.Textures
{
    public class AnimatedTextureComponent
    {
        public readonly string Texture;
        public readonly int MinTicks;
        public readonly int MaxTicks;
        public int TextureIndex;

        public AnimatedTextureComponent(string texture, int min, int max, int textureIndex = 0)
        {
            Precondition(!texture.Empty(), "Cannot have an empty texture component name");
            Precondition(min >= 0 && min <= max, "Min must be positive and max must not be less than min");
            
            Texture = texture.ToUpper();
            MinTicks = min;
            MaxTicks = max;
            TextureIndex = textureIndex;
        }

        public override string ToString() => $"{Texture} (startTicks={MinTicks} endTicks={MaxTicks})";
    }
}
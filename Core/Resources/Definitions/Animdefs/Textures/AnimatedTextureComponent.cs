using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Animdefs.Textures;

public sealed class AnimatedTextureComponent
{
    public string ConfiguredTexture;
    public int MinTicks;
    public int MaxTicks;
    public int ConfiguredTextureIndex;
    public int TextureIndex;
    public string Texture;

    public AnimatedTextureComponent(string texture, int min, int max, int textureIndex = 0)
    {
        Precondition(!texture.Empty(), "Cannot have an empty texture component name");
        Precondition(min >= 0 && min <= max, "Min must be positive and max must not be less than min");

        ConfiguredTexture = texture;
        Texture = texture;
        MinTicks = min;
        MaxTicks = max;
        ConfiguredTextureIndex = textureIndex;
        TextureIndex = textureIndex;
    }

    public override string ToString() => $"{Texture} (startTicks={MinTicks} endTicks={MaxTicks} index={ConfiguredTextureIndex})";
}

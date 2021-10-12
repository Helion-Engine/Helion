using Helion.Resources.Definitions.Animdefs.Textures;

namespace Helion.Resources;

public class Animation
{
    public AnimatedTexture AnimatedTexture;
    public int TranslationIndex;
    public int AnimationIndex;
    public int Tics;

    public Animation(AnimatedTexture animatedTexture, int textureIndex)
    {
        AnimatedTexture = animatedTexture;
        TranslationIndex = textureIndex;
    }

    public override string ToString() => $"{AnimatedTexture.Name} (Animation = {AnimationIndex}, Tick = {Tics})";
}


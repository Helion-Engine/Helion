using Helion.Resources.Definitions.Animdefs.Textures;

namespace Helion.Resources.TexturesNew.Animations;

public record ResourceAnimation(AnimatedTexture AnimatedTexture, int TranslationIndex, int AnimationIndex = 0, int Ticks = 0);
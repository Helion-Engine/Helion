using Helion.Resource.Textures;

namespace Helion.Worlds.Textures.Types
{
    /// <summary>
    /// A single frame of a texture animation. Each animation is made up of one
    /// or more of these frames.
    /// </summary>
    public class AnimatedTextureFrame
    {
        /// <summary>
        /// The texture for this frame.
        /// </summary>
        public readonly Texture Texture;

        /// <summary>
        /// How long this frame lasts.
        /// </summary>
        public readonly int DurationTicks;

        public AnimatedTextureFrame(Texture texture, int durationTicks)
        {
            Texture = texture;
            DurationTicks = durationTicks;
        }
    }
}

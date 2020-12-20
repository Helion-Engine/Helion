using Helion.Resource.Textures;

namespace Helion.Resource.Sprites
{
    /// <summary>
    /// A specific rotation in a sprite. This is the atomic image for one of
    /// the sprite angles. Each sprite is made up of these.
    /// </summary>
    public class SpriteRotation
    {
        /// <summary>
        /// The texture for this rotation.
        /// </summary>
        public readonly Texture Texture;

        /// <summary>
        /// True if this is a mirror texture.
        /// </summary>
        /// <remarks>
        /// This is a hint to the renderer to not upload this again, but
        /// to flip it when rendering.
        /// </remarks>
        public readonly bool Mirror;

        public SpriteRotation(Texture texture, bool mirror)
        {
            Texture = texture;
            Mirror = mirror;
        }

        public override string ToString() => $"{Texture.Name} (mirror: {Mirror}";
    }
}

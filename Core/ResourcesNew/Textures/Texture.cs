using Helion.Graphics;
using Helion.Util;

namespace Helion.ResourcesNew.Textures
{
    /// <summary>
    /// A texture that can be used in sprites and rendered with.
    /// </summary>
    public class Texture
    {
        /// <summary>
        /// Name of the texture.
        /// </summary>
        public readonly CIString Name;

        /// <summary>
        /// Cached image of the texture.
        /// </summary>
        public readonly Image Image;

        /// <summary>
        /// Resource namespace of the texture.
        /// </summary>
        public readonly Namespace Namespace;

        /// <summary>
        /// Index of the texture.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// If this texture is the "missing" texture, which means it represents
        /// a resource that could not be found.
        /// </summary>
        public readonly bool IsMissing;

        /// <summary>
        /// If this is a sky texture.
        /// </summary>
        public readonly bool IsSky;

        public Texture(CIString name, Image image, Namespace resourceNamespace, int index,
            bool isMissingTexture = false, bool isSkyTexture = false)
        {
            Name = name;
            Namespace = resourceNamespace;
            Image = image;
            Index = index;
            IsMissing = isMissingTexture;
            IsSky = isSkyTexture;
        }

        public override string ToString() => $"{Name} ({Namespace}) {Index} [Missing {IsMissing}, Sky {IsSky}]";
    }
}

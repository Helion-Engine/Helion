using Helion.Graphics;
using Helion.Util;

namespace Helion.ResourcesNew.Textures
{
    /// <summary>
    /// A loaded texture.
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

        public Texture(CIString name, Image image, Namespace resourceNamespace, int index)
        {
            Name = name;
            Namespace = resourceNamespace;
            Image = image;
            Index = index;
        }
    }
}

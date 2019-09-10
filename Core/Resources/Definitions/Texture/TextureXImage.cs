using System;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// A single image in a TextureX data structure.
    /// </summary>
    public class TextureXImage
    {
        /// <summary>
        /// The name of the texture.
        /// </summary>
        public readonly CIString Name;

        /// <summary>
        /// The width of the texture.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// The height of the texture.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// All the patches that make up the texture.
        /// </summary>
        public readonly List<TextureXPatch> Patches;
        
        /// <summary>
        /// Gets the dimension for this image.
        /// </summary>
        public Dimension Dimension => new Dimension(Width, Height);

        /// <summary>
        /// Creates a new texture1/2/3 image.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <param name="patches">All the patches that make up the texture.
        /// </param>
        public TextureXImage(CIString name, int width, int height, List<TextureXPatch> patches)
        {
            Precondition(width >= 0, "TextureX image width must not be negative");
            Precondition(height >= 0, "TextureX image height must not be negative");

            Name = name;
            Patches = patches;
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }
    }
}
using System;
using System.Drawing;
using Helion.Resource;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using SixLabors.ImageSharp.Metadata;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Palette
{
    /// <summary>
    /// A 2-dimensional raster image of palette indices.
    /// </summary>
    /// <remarks>
    /// This uses 16-bit indices instead of 8-bit indices so we can represent
    /// transparency. Palette images will frequently need to be converted to
    /// an image, and this format makes it easy to do so. It also will be used
    /// in editors, and this makes it a lot easier to use X and Y coordinates
    /// to set new index values without doing a bunch of math every time this
    /// gets some kind of 'pixel' update.
    /// </remarks>
    public class PaletteImage
    {
        /// <summary>
        /// The index used to represent transparency.
        /// </summary>
        public const ushort TransparentIndex = 0XFFFF;

        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The indices that make up the 2D raster. This is a single array, but
        /// it is in row-major format and it's length is equal to the width by
        /// the height.
        /// </summary>
        public ushort[] Indices { get; }

        /// <summary>
        /// The offset of the image. These are offsets for the engine to apply
        /// to images when rendering.
        /// </summary>
        public Vec2I Offset { get; set; } = Vec2I.Zero;

        /// <summary>
        /// The namespace this image was located in.
        /// </summary>
        public Namespace Namespace { get; set; } = Namespace.Global;

        /// <summary>
        /// Creates a palette image with the index provided.
        /// </summary>
        /// <param name="width">The image width. Should be positive.</param>
        /// <param name="height">The image height, should be positive.</param>
        /// <param name="fillIndex">The index to fill the image with.</param>
        /// <param name="resourceNamespace">The namespace for this image.
        /// </param>
        /// <param name="offset">The offset (or null if (0, 0) should be used.
        /// </param>
        public PaletteImage(int width, int height, ushort fillIndex, Namespace resourceNamespace = Namespace.Global,
            Vec2I? offset = null)
        {
            Precondition(width >= 1, "Palette image width should be positive");
            Precondition(height >= 1, "Palette image height be positive");

            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Indices = new ushort[width * height];
            Namespace = resourceNamespace;
            Offset = offset ?? Vec2I.Zero;

            Fill(fillIndex);
        }

        /// <summary>
        /// Creates a palette image from the data provided.
        /// </summary>
        /// <remarks>
        /// This is a safe function such that if the width and height are out
        /// of range. This will create a default filled function and not crash
        /// if the data is invalid. It is up to the caller to make sure that
        /// the data is valid, but we take extra steps to prevent disastrous
        /// results from happening because this operates off of raw data.
        /// </remarks>
        /// <param name="width">The image width. Should be positive.</param>
        /// <param name="height">The image height, should be positive.</param>
        /// <param name="indices">The index data, which should have a length
        /// equal to the width times the height.</param>
        /// <param name="resourceNamespace">The namespace for this image.
        /// </param>
        /// <param name="offset">The offset (or null if (0, 0) should be used.
        /// </param>
        public PaletteImage(int width, int height, ushort[] indices, Namespace resourceNamespace = Namespace.Global,
            Vec2I? offset = null)
        {
            Precondition(width * height == indices.Length, "Palette image indices and width/height mismatch");

            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Namespace = resourceNamespace;
            Offset = offset ?? Vec2I.Zero;

            if (width * height == indices.Length)
                Indices = indices;
            else
            {
                ushort[] newIndices = new ushort[Width * Height];
                newIndices.Fill(TransparentIndex);
                Indices = newIndices;
            }
        }

        /// <summary>
        /// Fills the palette indices with the provided index.
        /// </summary>
        /// <param name="index">The index to set everywhere in the raster.
        /// </param>
        public void Fill(ushort index)
        {
            int offset = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Indices[offset++] = index;
        }

        /// <summary>
        /// Converts the palette image to a palette using the topmost layer
        /// of the palette.
        /// </summary>
        /// <param name="palette">The palette to use.</param>
        /// <returns>The image from the palette.</returns>
        public Image ToImage(Palette palette)
        {
            byte[] argb = new byte[Width * Height * 4];
            Color[] paletteLayer = palette[0];

            // OPTIMIZE: Unsafe code with pointers is probably worth doing!
            int argbOffset = 0;
            int indexOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ushort index = Indices[indexOffset];

                    if (index >= 256)
                    {
                        for (int i = 0; i < 4; i++)
                            argb[argbOffset + i] = 0x00;
                    }
                    else
                    {
                        Color color = paletteLayer[index];
                        argb[argbOffset] = color.B;
                        argb[argbOffset + 1] = color.G;
                        argb[argbOffset + 2] = color.R;
                        argb[argbOffset + 3] = 0xFF;
                    }

                    indexOffset++;
                    argbOffset += 4;
                }
            }

            return new Image(Width, Height, argb)
            {
                Namespace = Namespace,
                Offset = Offset
            };
        }
    }
}

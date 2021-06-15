using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Resources;
using Helion.Util.Extensions;

namespace Helion.Graphics.New
{
    /// <summary>
    /// An image backed on indexed data from some palette.
    /// </summary>
    public class PaletteImage : IImage
    {
        public const ushort TransparentIndex = 0x0100;

        public readonly ushort[] Indices;
        public Bitmap Bitmap { get; } = null!; // TODO: Bye
        public Dimension Dimension { get; }
        public int Width => Dimension.Width;
        public int Height => Dimension.Height;
        public ImageType ImageType => ImageType.Palette;
        public Vec2I Offset { get; }
        public ResourceNamespace Namespace { get; }

        public PaletteImage(Dimension dimension, ushort fillIndex = TransparentIndex, Vec2I offset = default, 
            ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            Dimension = new Dimension(dimension.Width.Max(1), dimension.Height.Max(1));
            Indices = new ushort[Dimension.Area];
            Offset = offset;
            Namespace = resourceNamespace;

            Fill(fillIndex);
        }
        
        /// <summary>
        /// Fills the palette indices with the provided index.
        /// </summary>
        /// <param name="index">The index to set everywhere in the raster.
        /// </param>
        public void Fill(ushort index)
        {
            int offset = 0;
            (int w, int h) = Dimension;
            
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    Indices[offset++] = index;
        }
        
        /// <summary>
        /// Draws the current image on top of the first argument, at the offset
        /// provided.
        /// </summary>
        /// <param name="image">The image on the bottom (destination), meaning it
        /// will have the current image drawn on top of this.</param>
        /// <param name="offset">The offset to which the image will be drawn
        /// at.</param>
        public void DrawOnTopOf(PaletteImage image, Vec2I offset)
        {
            // This makes sure that we are at least drawing one pixel. This is
            // an invariant used throughout the code later on.
            if (offset.X >= Width || offset.Y >= Height || offset.X + image.Width <= 0 || offset.Y + image.Height <= 0)
                return;

            ushort[] src = Indices;
            ushort[] dest = image.Indices;
            Vec2I srcStart = Vec2I.Zero;
            Vec2I srcEnd = (Width, Height);
            Vec2I destStart = (offset.X.Max(0), offset.Y.Max(0));

            // If the offset is negative, set our starting point inside of the
            // source image so it matches up with (0, 0) of the destination.
            if (offset.X < 0)
                srcStart.X -= offset.X;
            if (offset.Y < 0)
                srcStart.Y -= offset.Y;

            // Calculate how many pixels we want to draw in each direction by
            // the source image. Then see if we'd run over the bounds of the
            // destination, and if so, reduce the range.
            // For example, if we want to draw 4 pixels, but our available size
            // is only 3 pixels, then we must only draw 3 pixels.
            Vec2I drawSize = srcEnd - srcStart;
            Vec2I availableSize = image.Dimension.Vector - destStart;
            if (drawSize.X > availableSize.X)
                srcEnd.X = srcStart.X + availableSize.X;
            if (drawSize.Y > availableSize.Y)
                srcEnd.Y = srcStart.Y + availableSize.Y;

            int destY = destStart.Y;
            for (int srcY = srcStart.Y; srcY < srcEnd.Y; srcY++)
            {
                int srcOffset = (srcY * Width) + srcStart.X;
                int destOffset = (destY * image.Width) + destStart.X;
                
                for (int srcX = srcStart.X; srcX < srcEnd.X; srcX++)
                {
                    if (src[srcOffset] != TransparentIndex)
                        dest[destOffset] = src[srcOffset];
                    
                    srcOffset++;
                    destOffset++;
                }

                destY++;
            }
        }
        
        /// <summary>
        /// Converts the palette image to a palette using the topmost layer
        /// of the palette.
        /// </summary>
        /// <param name="palette">The palette to use.</param>
        /// <returns>The image from the palette.</returns>
        public ArgbImage ToRgbaImage(Palette palette)
        {
            (int w, int h) = Dimension;
            byte[] argb = new byte[Dimension.Area * 4];
            Color[] paletteLayer = palette[0];

            int argbOffset = 0;
            int indexOffset = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    ushort index = Indices[indexOffset];
            
                    // Note: This assumes that we never use more than 256
                    // incides, and that anything beyond this is transparent.
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

            int numBytes = w * h * 4;
            Bitmap bitmap = new(w, h, PixelFormat.Format32bppArgb);
            
            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(argb, 0, data.Scan0, numBytes);
            bitmap.UnlockBits(data);

            return new ArgbImage(bitmap, Offset, Namespace);
        }
        
        /// <summary>
        /// Saves this image to the hard drive at the path provided.
        /// </summary>
        /// <param name="path">The path to save it at.</param>
        /// <returns>True on success, false on failure.</returns>
        public bool WriteToDisk(string path)
        {
            try
            {
                byte[] data = new byte[Indices.Length * sizeof(ushort)];
                Buffer.BlockCopy(Indices, 0, data, 0, data.Length);
                File.WriteAllBytes(path, data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

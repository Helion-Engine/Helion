using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.New
{
    /// <summary>
    /// An image that stores ARGB data internally.
    /// </summary>
    public class ArgbImage : IImage
    {
        public readonly Bitmap Bitmap;
        public Dimension Dimension => (Width, Height);
        public int Width { get; }
        public int Height { get; }
        public ImageType ImageType => ImageType.Argb;
        public Vec2I Offset { get; }
        public ResourceNamespace Namespace { get; }

        public ArgbImage(Bitmap bitmap, Vec2I offset = default, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            Precondition(bitmap.PixelFormat == PixelFormat.Format32bppArgb, "Only support 32-bit ARGB image formats");
            
            Bitmap = bitmap;
            Width = bitmap.Width;
            Height = bitmap.Height;
            Offset = offset;
            Namespace = resourceNamespace;
        }

        public ArgbImage(Dimension dimension, Color color, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            Width = Math.Max(dimension.Width, 1);
            Height = Math.Max(dimension.Height, 1);
            Bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            Offset = Vec2I.Zero;
            Namespace = resourceNamespace;
            
            Fill(color);
        }

        /// <summary>
        /// Creates an image from the ARGB data and dimensions provided.
        /// </summary>
        /// <remarks>
        /// If there is a data mismatch (such as 4 * w * h != data length) then
        /// null is returned.
        /// </remarks>
        /// <param name="dimension">The dimension of the image.</param>
        /// <param name="argb">The raw ARGB data. Due to little endianness, the
        /// lower byte may have to be blue and the highest order byte alpha.
        /// </param>
        /// <param name="offset">The offset (zero by default).</param>
        /// <param name="resourceNamespace">The resource namespace.</param>
        /// <returns>The image, or null if the image cannot be made due to data
        /// being of an incorrect size.</returns>
        public static ArgbImage? FromArgbBytes(Dimension dimension, byte[] argb, Vec2I offset = default,
            ResourceNamespace resourceNamespace = ResourceNamespace.Global)
        {
            (int w, int h) = dimension;
            int numBytes = w * h * 4;
            
            if (argb.Length != numBytes || w <= 0 || h <= 0)
                return null;
            
            Bitmap bitmap = new(dimension.Width, dimension.Height, PixelFormat.Format32bppArgb);
            
            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(argb, 0, data.Scan0, numBytes);
            bitmap.UnlockBits(data);

            return new ArgbImage(bitmap, offset, resourceNamespace);
        }
        
        /// <summary>
        /// Fills the image with the color provided.
        /// </summary>
        /// <param name="color">The color to fill.</param>
        public void Fill(Color color)
        {
            using SolidBrush b = new(color);
            using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Bitmap);
            g.FillRectangle(b, 0, 0, Bitmap.Width, Bitmap.Height);
        }
        
        /// <summary>
        /// Draws the current image on top of the first argument, at the offset
        /// provided.
        /// </summary>
        /// <param name="image">The image on the bottom, meaning it will have
        /// the current image drawn on top of this.</param>
        /// <param name="offset">The offset to which the image will be drawn
        /// at.</param>
        public void DrawOnTopOf(ArgbImage image, Vec2I offset)
        {
            using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image.Bitmap);
            g.DrawImage(Bitmap, offset.X, offset.Y);
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
                Bitmap.Save(path, ImageFormat.Png);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

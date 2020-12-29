using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics
{
    /// <summary>
    /// A 32-bit ARGB image that can be used with both any windowed forms and
    /// also compatible for being transferred to any GPU driver.
    /// </summary>
    ///
    public class Image
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width => Bitmap.Width;

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height => Bitmap.Height;

        /// <summary>
        /// Calculates the dimension of the bitmap.
        /// </summary>
        public Dimension Dimension => new Dimension(Width, Height);

        /// <summary>
        /// The raw data for the image.
        /// </summary>
        public Bitmap Bitmap { get; }

        /// <summary>
        /// The extra data that accompanies the pixels/dimensions.
        /// </summary>
        public ImageMetadata Metadata { get; } = new ImageMetadata();

        /// <summary>
        /// Creates an image from an existing bitmap.
        /// </summary>
        /// <remarks>
        /// If the bitmap cannot be converted to 32-bit ARGB, then the image
        /// will be a default generated 1x1 image.
        /// </remarks>
        /// <param name="bitmap">The bitmap to use, ideally should be in 32-bit
        /// ARGB format.</param>
        /// <param name="metadata">Optional metadata. If null is provided, then
        /// a default value will be constructed for this object.</param>
        public Image(Bitmap bitmap, ImageMetadata? metadata = null)
        {
            Bitmap = EnsureInArgbFormat(bitmap);

            if (metadata != null)
                Metadata = metadata;
        }

        /// <summary>
        /// Creates a image with the width/height provided and all filled with
        /// opaque black.
        /// </summary>
        /// <param name="width">The width of the image. Should be positive.
        /// </param>
        /// <param name="height">The height of the image. Should be positive.
        /// </param>
        public Image(int width, int height) : this(width, height, Color.Black)
        {
        }

        /// <summary>
        /// Creates an image from the information provided.
        /// </summary>
        /// <param name="width">The width of the image. Should be positive.
        /// </param>
        /// <param name="height">The height of the image. Should be positive.
        /// </param>
        /// <param name="color">The color to fill the image with.</param>
        /// <param name="metadata">Optional metadata. If null is provided, then
        /// a default value will be constructed for this object.</param>
        public Image(int width, int height, Color color, ImageMetadata? metadata = null)
        {
            Precondition(width > 0, "Trying to make a non-positive width image");
            Precondition(height > 0, "Trying to make a non-positive height image");

            if (metadata != null)
                Metadata = metadata;

            Bitmap = new Bitmap(Math.Max(0, width), Math.Max(0, height), PixelFormat.Format32bppArgb);
            Fill(color);
        }

        /// <summary>
        /// Creates an image from the ARGB data and dimensions provided.
        /// </summary>
        /// <remarks>
        /// If there is a data mismatch (such as 4 * w * h != data length) then
        /// a default 1x1 image is created instead.
        /// </remarks>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="argb">The raw ARGB data. Due to little endianness, the
        /// lower byte may have to be blue and the highest order byte alpha.
        /// </param>
        /// <param name="metadata">Optional metadata. If null is provided, then
        /// a default value will be constructed for this object.</param>
        public Image(int width, int height, byte[] argb, ImageMetadata? metadata = null)
        {
            Precondition(width >= 0, "Trying to make a negative width image");
            Precondition(height >= 0, "Trying to make a negative height image");
            Precondition(width * height * 4 == argb.Length, "ARGB pixel array width/height mismatch");

            Bitmap = new Bitmap(Math.Max(0, width), Math.Max(0, height), PixelFormat.Format32bppArgb);

            if (metadata != null)
                Metadata = metadata;

            // Because we're going to be doing potentially dangerous actions
            // for speed reasons, we need to make sure we only do safe things.
            int numBytes = width * height * 4;
            if (numBytes != argb.Length)
                return;

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData data = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(argb, 0, data.Scan0, numBytes);
            Bitmap.UnlockBits(data);
        }

        /// <summary>
        /// Fills the image with the color provided.
        /// </summary>
        /// <param name="color">The color to fill.</param>
        public void Fill(Color color)
        {
            using (SolidBrush b = new SolidBrush(color))
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Bitmap))
                    g.FillRectangle(b, 0, 0, Bitmap.Width, Bitmap.Height);
        }

        public Image ToBrightnessCopy()
        {
            Precondition(Bitmap.PixelFormat == PixelFormat.Format32bppArgb, "Unsupported pixel format type");

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            BitmapData data = Bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int numBytes = 4 * Width * Height;
            byte[] argb = new byte[numBytes];
            Marshal.Copy(data.Scan0, argb, 0, numBytes);

            Bitmap.UnlockBits(data);

            for (int i = 0; i < numBytes; i += 4)
            {
                // Note: Because of endianness, ARGB is read in BGRA format.
                // This means we need the first 3 bytes, and not the last 3.
                byte maxRGB = Math.Max(Math.Max(argb[i], argb[i + 1]), argb[i + 2]);
                argb[i] = maxRGB;
                argb[i + 1] = maxRGB;
                argb[i + 2] = maxRGB;
            }

            return new Image(Width, Height, argb, Metadata);
        }

        /// <summary>
        /// Draws the current image on top of the first argument, at the offset
        /// provided.
        /// </summary>
        /// <param name="image">The image on the bottom, meaning it will have
        /// the current image drawn on top of this.</param>
        /// <param name="offset">The offset to which the image will be drawn
        /// at.</param>
        public void DrawOnTopOf(Image image, Vec2I offset)
        {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image.Bitmap))
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
                Log.Error($"Unable to save image to {path}");
                return false;
            }
        }

        private static Bitmap MakeDefaultBitmap() => new Bitmap(1, 1, PixelFormat.Format32bppArgb);

        private static Bitmap EnsureInArgbFormat(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
                return bitmap;

            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(newBitmap))
                    graphics.DrawImage(bitmap, 0, 0);
                return newBitmap;
            }
            catch (Exception e)
            {
                Log.Warn($"Unable to convert bitmap from {bitmap.PixelFormat} to a 32-bit ARGB raster: {e.Message}");
                return MakeDefaultBitmap();
            }
        }
    }
}
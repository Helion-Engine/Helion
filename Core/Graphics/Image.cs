using Helion.Util;
using NLog;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static Helion.Util.Assert;

namespace Helion.Graphics
{
    /// <summary>
    /// A 32-bit ARGB image that can be used with both any windowed forms and
    /// also compatible for being transferred to any GPU driver.
    /// </summary>
    public class Image
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width => Bitmap.Width;

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height => Bitmap.Height;

        /// <summary>
        /// The raw data for the image.
        /// </summary>
        public Bitmap Bitmap { get; } = MakeDefaultBitmap();

        /// <summary>
        /// The extra data that accompanies the pixels/dimensions.
        /// </summary>
        public ImageMetadata Metadata { get; } = new ImageMetadata();

        /// <summary>
        /// Creates an image which has a default bitmap.
        /// </summary>
        public Image()
        {
        }

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
        public Image(Bitmap bitmap, ImageMetadata metadata = null)
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
        public Image(int width, int height, Color color, ImageMetadata metadata = null)
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
        public Image(int width, int height, byte[] argb, ImageMetadata metadata = null)
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

        private static Bitmap MakeDefaultBitmap() => new Bitmap(1, 1, PixelFormat.Format32bppArgb);

        private static Bitmap EnsureInArgbFormat(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
                return bitmap;

            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(newBitmap))
                    graphics.DrawImage(bitmap, new Point(0, 0));
                return newBitmap;
            }
            catch (Exception e)
            {
                log.Warn($"Unable to convert bitmap from {bitmap.PixelFormat} to a 32-bit ARGB raster: {e.Message}");
                return MakeDefaultBitmap();
            }
        }

        /// <summary>
        /// Fills the image with the color provided.
        /// </summary>
        /// <param name="color">The color to fill.</param>
        public void Fill(Color color)
        {
            // TODO: This should almost certainly be done natively or in some
            // better way, this is probably slow.
            for (int row = 0; row < Bitmap.Height; row++)
                for (int col = 0; col < Bitmap.Width; col++)
                    Bitmap.SetPixel(col, row, color);
        }
    }
}

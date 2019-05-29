using Helion.Graphics;
using System;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Shared
{
    public abstract class GLTextureManager : IDisposable
    {
        public abstract void Dispose();

        protected abstract void PerformTextureUpload(Image image, IntPtr dataPtr);

        /// <summary>
        /// Calculates the proper max mipmap levels for the image.
        /// </summary>
        /// <param name="image">The image to get the mipmap levels for.</param>
        /// <returns>The best mipmap level value.</returns>
        protected int CalculateMaxMipmapLevels(Image image)
        {
            Precondition(image.Width > 0 && image.Height > 0, "Cannot make mipmap from a zero dimension image");

            int minDimension = Math.Min(image.Width, image.Height);
            int levels = (int)Math.Log(minDimension, 2.0);
            return Math.Max(1, levels);
        }

        /// <summary>
        /// Uploads the image data to the currently bound 2D texture.
        /// </summary>
        /// <param name="image">The image to upload.</param>
        protected void UploadTexturePixels(Image image)
        {
            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            System.Drawing.Imaging.BitmapData bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);

            PerformTextureUpload(image, bitmapData.Scan0);

            image.Bitmap.UnlockBits(bitmapData);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Resources;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics;

/// <summary>
/// An image, that can either contain ARGB data or palette indices.
/// </summary>
/// <remarks>
/// The bitmap, if of ImageType Palette, will have only the alpha and red
/// channels set. The alpha channel will be either 255, or 0.
/// </remarks>
public class Image
{
    public const ushort TransparentIndex = 0xFF00;
    public static readonly Image NullImage = CreateNullImage();
    public static readonly Image WhiteImage = CreateWhiteImage();

    public readonly Bitmap Bitmap;
    public readonly Dimension Dimension;
    public readonly ImageType ImageType;
    public readonly Vec2I Offset;
    public readonly ResourceNamespace Namespace;

    public int Width => Dimension.Width;
    public int Height => Dimension.Height;

    /// <summary>
    /// Creates a new image that uses the bitmap provided. If it is not in
    /// 32bpp ARGB, it will be converted.
    /// </summary>
    /// <param name="bitmap">The bitmap to use.</param>
    /// <param name="imageType">The image type.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="resourceNamespace">The resource namespace.</param>
    public Image(Bitmap bitmap, ImageType imageType, Vec2I offset = default, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
    {
        Bitmap = EnsureExpectedFormat(bitmap);
        ImageType = imageType;
        Dimension = (bitmap.Width, bitmap.Height);
        Offset = offset;
        Namespace = resourceNamespace;
    }

    /// <summary>
    /// Creates a new image filled with some color (transparent by default).
    /// </summary>
    /// <param name="width">The width (if less than 1, will be set to 1).
    /// </param>
    /// <param name="height">The height (if less than 1, will be set to 1).
    /// </param>
    /// <param name="imageType">The image type to use.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="resourceNamespace">The resource namespace.</param>
    /// <param name="fillColor">The color to use, or transparent by default.
    /// </param>
    public Image(int width, int height, ImageType imageType, Vec2I offset = default,
        ResourceNamespace resourceNamespace = ResourceNamespace.Global, Color? fillColor = null)
    {
        Dimension = (Math.Max(width, 1), Math.Max(height, 1));
        Bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        ImageType = imageType;
        Offset = offset;
        Namespace = resourceNamespace;

        Fill(fillColor ?? Color.Transparent);
    }

    private static Bitmap EnsureExpectedFormat(Bitmap bitmap)
    {
        if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
            return bitmap;

        Bitmap copy = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

        using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(copy);
        g.DrawImage(bitmap, new Rectangle(0, 0, copy.Width, copy.Height));

        return copy;
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
    public static Image? FromArgbBytes(Dimension dimension, byte[] argb, Vec2I offset = default,
        ResourceNamespace resourceNamespace = ResourceNamespace.Global)
    {
        (int w, int h) = dimension;
        int numBytes = w * h * 4;

        if (argb.Length != numBytes || w <= 0 || h <= 0)
            return null;

        Bitmap bitmap = new(w, h, PixelFormat.Format32bppArgb);

        Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData metadata = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        Marshal.Copy(argb, 0, metadata.Scan0, numBytes);
        bitmap.UnlockBits(metadata);

        return new Image(bitmap, ImageType.Argb, offset, resourceNamespace);
    }

    public static Image? FromPaletteIndices(Dimension dimension, ushort[] indices, Vec2I offset = default,
        ResourceNamespace resourceNamespace = ResourceNamespace.Global)
    {
        (int w, int h) = dimension;

        if (w <= 0 || h <= 0 || indices.Length != w * h)
            return null;

        int numBytes = w * h * 4;
        byte[] paletteData = new byte[numBytes];

        // TODO: Perf: Save time and have the user pass in the AxxI format.
        int argbIndex = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            ushort index = indices[i];

            // To avoid branching: The index is either 0x0000 -> 0x00FF, but
            // if it's transparent then it's 0xFF00. Since 0 -> 255 needs a
            // 0xFF for the alpha (but the upper byte is 0x00), and since the
            // translucent index is 0xFF00 (so the upper byte is 0xFF), then
            // the alpha is equal to taking the top byte and flipping it.
            //
            // This is also stupid and will likely be removed when the to do
            // comment above has the caller passing in an already allocated ARGB
            // data buffer.
            paletteData[argbIndex] = (byte)~(index >> 8);
            paletteData[argbIndex + 3] = (byte)index;

            argbIndex += 4;
        }

        Bitmap bitmap = new(w, h, PixelFormat.Format32bppArgb);
        Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData metadata = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
        Marshal.Copy(paletteData, 0, metadata.Scan0, numBytes);
        bitmap.UnlockBits(metadata);

        return new Image(bitmap, ImageType.Palette, offset, resourceNamespace);
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
    public void DrawOnTopOf(Image image, Vec2I offset)
    {
        using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image.Bitmap);
        g.DrawImage(Bitmap, offset.X, offset.Y);
    }

    /// <summary>
    /// Converts the palette image to an ARGB image. If this image is
    /// already in ARGB, it will return itself and nothing new will be
    /// allocated.
    /// </summary>
    /// <param name="palette">The palette to convert with.</param>
    /// <returns>The converted image, or itself if it is ARGB.</returns>
    public Image PaletteToArgb(Palette palette)
    {
        if (ImageType == ImageType.Argb)
            return this;

        int numBytes = Width * Height * 4;
        byte[] paletteBytes = new byte[numBytes];
        byte[] argbBytes = new byte[numBytes];

        Rectangle rect = new(0, 0, Bitmap.Width, Bitmap.Height);
        BitmapData metadata = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
        Marshal.Copy(metadata.Scan0, paletteBytes, 0, numBytes);
        Bitmap.UnlockBits(metadata);

        Color[] colors = palette.DefaultLayer;
        int offset = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // The first of the four bytes is the alpha, and if we have
                // alpha, then we have to write the RGB. Otherwise, it is
                // already set to transparent so our job would be done.
                if (paletteBytes[offset] != 0)
                {
                    int index = paletteBytes[offset + 3];
                    Color color = colors[index];

                    // Apparently since it reads it as a 32-bit word, then
                    // we need to write it in BGRA format.
                    argbBytes[offset] = color.B;
                    argbBytes[offset + 1] = color.G;
                    argbBytes[offset + 2] = color.R;
                    argbBytes[offset + 3] = 255;
                }

                offset += 4;
            }
        }

        Image? image = FromArgbBytes((Width, Height), argbBytes, Offset, Namespace);
        if (image != null)
            return image;

        Fail("Should never fail to convert to ARGB from palette at this point");
        return new Image(1, 1, ImageType.Argb);
    }

    /// <summary>
    /// Saves this image to the hard drive at the path provided.
    /// </summary>
    /// <remarks>
    /// Note that palette images will be written based on their "AR" color
    /// channel, meaning it will be a bunch of black to red pixels, and
    /// any transparency will be either 255 or 0 for the alpha channel. It
    /// will not write them as a doom column byte formatted file.
    /// </remarks>
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

    /// <summary>
    /// Creates a checkered red/black null image.
    /// </summary>
    /// <returns>The 8x8 image that represents a null or missing image.</returns>
    private static Image CreateNullImage()
    {
        const int dimension = 8;
        const int halfDimension = dimension / 2;

        Bitmap bitmap = new(dimension, dimension, PixelFormat.Format32bppArgb);

        for (int y = 0; y < dimension; y++)
            for (int x = 0; x < dimension; x++)
                bitmap.SetPixel(x, y, Color.Black);

        for (int y = 0; y < halfDimension; y++)
            for (int x = 0; x < halfDimension; x++)
                bitmap.SetPixel(x, y, Color.Red);

        for (int y = halfDimension; y < dimension; y++)
            for (int x = halfDimension; x < dimension; x++)
                bitmap.SetPixel(x, y, Color.Red);

        return new Image(bitmap, ImageType.Argb);
    }

    private static Image CreateWhiteImage()
    {
        Bitmap bitmap = new(1, 1, PixelFormat.Format32bppArgb);
        bitmap.SetPixel(1, 1, Color.White);
        return new(bitmap, ImageType.Argb);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Graphics.Palettes;
using Helion.Maps;
using Helion.Resources;
using Helion.Util.Assertion;
using Helion.Util.Extensions;
using Newtonsoft.Json.Linq;
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

    public Dimension Dimension;
    public readonly ImageType ImageType;
    public readonly Vec2I Offset;
    public readonly ResourceNamespace Namespace;
    private readonly uint[] m_pixels;

    public int Width => Dimension.Width;
    public int Height => Dimension.Height;
    public Span<uint> Pixels => m_pixels;

    public Image(Dimension dimension, ImageType imageType, Vec2I offset = default, ResourceNamespace ns = ResourceNamespace.Global) :
        this(new uint[dimension.Area], dimension, imageType, offset, ns)
    {
    }

    public Image(int w, int h, ImageType imageType, Vec2I offset = default, ResourceNamespace ns = ResourceNamespace.Global) :
        this((w, h), imageType, offset, ns)
    {
        Precondition(w >= 0, "Tried providing a negative width for an image");
        Precondition(h >= 0, "Tried providing a negative height for an image");
    }

    public Image(uint[] pixels, Dimension dimension, ImageType imageType, Vec2I offset, ResourceNamespace ns)
    {
        Precondition(pixels.Length == dimension.Area, "Image size mismatch");

        Dimension = dimension;
        ImageType = imageType;
        Offset = offset;
        Namespace = ns;
        m_pixels = pixels;
    }

    public static Image? FromPaletteIndices(Dimension dimension, ushort[] indices, Vec2I offset = default, ResourceNamespace ns = ResourceNamespace.Global)
    {
        if (dimension.Area != indices.Length)
            return null;

        uint[] pixels = indices.Select(s => (uint)s).ToArray();
        return new(pixels, dimension, ImageType.Palette, offset, ns);
    }

    public static Image? FromArgbBytes(Dimension dimension, byte[] argbData, Vec2I offset = default, ResourceNamespace ns = ResourceNamespace.Global)
    {
        if (dimension.Area != argbData.Length * 4)
            return null;

        uint[] pixels = new uint[argbData.Length / 4];

        int argbByteOffset = 0;
        for (int i = 0; i < argbData.Length; i++)
        {
            uint a = argbData[argbByteOffset];
            uint r = argbData[argbByteOffset + 1];
            uint g = argbData[argbByteOffset + 2];
            uint b = argbData[argbByteOffset + 3];
            pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
            argbByteOffset += 4;
        }

        return new(pixels, dimension, ImageType.Argb, offset, ns);
    }

    public Image PaletteToArgb(Palette palette)
    {
        uint[] pixels = new uint[m_pixels.Length];
        Color[] layer = palette.DefaultLayer;

        for (int i = 0; i < m_pixels.Length; i++)
        {
            uint argb = m_pixels[i];
            pixels[i] = (argb == Image.TransparentIndex ? Color.Transparent.Uint : layer[argb].Uint);
        }

        return new(pixels, Dimension, ImageType.Argb, Offset, Namespace);
    }

    public void DrawOnTopOf(Image image, Vec2I offset)
    {
        int writeXStart = offset.X;
        int writeXEnd = writeXStart + image.Width;
        int writeYStart = offset.Y;
        int writeYEnd = writeYStart + image.Height;

        // Do we write any pixels at all? If we're fully outside, such as too
        // far to the left/right/up/down, we can't draw anything.
        if (writeXStart >= Width || writeXEnd <= 0 || writeYStart >= Height || writeYEnd <= 0)
            return;

        int thisOffset = 0;
        for (int y = 0; y < Height; y++)
        {
            int targetOffset = (offset.Y * image.Width) + offset.X;
            int targetX = offset.X;

            // If this row is above the image as we draw downward, continue on
            // because maybe we will have some rows that intersect the image.
            if (targetOffset < 0)
                continue;

            // Since we draw from the top downward, if we've gone past the bottom,
            // then there's no more rows to draw.
            if (targetOffset >= image.m_pixels.Length)
                return;
            
            for (int x = 0; x < Width; x++)
            {
                // If we've written too far to the right and would go out of bounds,
                // stop blitting this row.
                if (targetX >= Width)
                    break;

                // If we're inside the image, copy the pixel over. This is gated by
                // the above conditional so that we know we're writing in the range
                // of [0, width).
                if (targetX >= 0)
                    image.m_pixels[targetOffset] = m_pixels[thisOffset];

                thisOffset++;
                targetOffset++;
                targetX++;
            }
        }
    }

    public void Fill(Color color)
    {
        m_pixels.Fill(color.Uint);
    }

    public void FillRows(Color color, int startY, int endY)
    {
        int offsetStart = startY * Width;
        int offsetEnd = (endY - startY) * Width;
        uint argb = color.Uint;
        for (int i = offsetStart; i < offsetEnd; i++)
            m_pixels[i] = argb;
    }

    public int TransparentPixelCount()
    {
        return m_pixels.Sum(p => (p & 0xFF000000) == 0 ? 1 : 0);
    }

    public Color GetPixel(int x, int y)
    {
        int offset = (y * Width) + x;
        uint argb = m_pixels[offset];
        return new(argb);
    }

    public void SetPixel(int x, int y, Color color)
    {
        int offset = (y * Width) + x;
        if (offset >= 0 && offset < m_pixels.Length)
            m_pixels[offset] = color.Uint;
    }

    private static Image CreateNullImage()
    {
        const int Dimension = 8;
        const int HalfDimension = Dimension / 2;

        Image image = new((Dimension, Dimension), ImageType.Argb);
        image.Fill(Color.Black);

        for (int y = 0; y < HalfDimension; y++)
            for (int x = 0; x < HalfDimension; x++)
                image.SetPixel(x, y, Color.Red);

        for (int y = HalfDimension; y < Dimension; y++)
            for (int x = HalfDimension; x < Dimension; x++)
                image.SetPixel(x, y, Color.Red);

        return image;
    }

    private static Image CreateWhiteImage()
    {
        return new(new[] { Color.White.Uint }, (1, 1), ImageType.Argb, (0, 0), ResourceNamespace.Global);
    }

    // From: https://swharden.com/blog/2022-11-04-csharp-create-bitmap/
    private byte[] GetBytes()
    {
        const int imageHeaderSize = 54;

        byte[] rgb = new byte[Dimension.Area * 3];

        int offset = 0;
        for (int i = 0; i < m_pixels.Length; i++)
        {
            uint argb = m_pixels[i];
            rgb[offset] = (byte)((argb & 0x00FF0000) >> 16);
            rgb[offset + 1] = (byte)((argb & 0x0000FF00) >> 8);
            rgb[offset + 2] = ((byte)(argb & 0x000000FF));
            offset += 3;
        }

        byte[] bmpBytes = new byte[rgb.Length + imageHeaderSize];

        bmpBytes[0] = (byte)'B';
        bmpBytes[1] = (byte)'M';
        bmpBytes[14] = 40;
        Array.Copy(BitConverter.GetBytes(bmpBytes.Length), 0, bmpBytes, 2, 4);
        Array.Copy(BitConverter.GetBytes(imageHeaderSize), 0, bmpBytes, 10, 4);
        Array.Copy(BitConverter.GetBytes(Width), 0, bmpBytes, 18, 4);
        Array.Copy(BitConverter.GetBytes(Height), 0, bmpBytes, 22, 4);
        Array.Copy(BitConverter.GetBytes(32), 0, bmpBytes, 28, 2);
        Array.Copy(BitConverter.GetBytes(rgb.Length), 0, bmpBytes, 34, 4);
        Array.Copy(rgb, 0, bmpBytes, imageHeaderSize, rgb.Length);

        return bmpBytes;
    }

    public bool Save(string path)
    {
        try
        {
            byte[] data = GetBytes();
            File.WriteAllBytes(path, data);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

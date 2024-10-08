using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

namespace Helion.Graphics;

public static class ImageExtensions
{
    public static bool SavePng(this Image image, string path)
    {
        try
        {
            var pixels = image.Pixels;
            byte[] data = new byte[pixels.Length * 4]; // rgba -> [r, g, b]
            for (int i = 0; i < pixels.Length; i++)
            {
                uint pixel = pixels[i];
                byte r = (byte)((pixel & 0x00FF0000) >> 16);
                byte g = (byte)((pixel & 0x0000FF00) >> 8);
                byte b = (byte)(pixel & 0x000000FF);

                int offset = i * 4;
                data[offset] = r;
                data[offset + 1] = g;
                data[offset + 2] = b;
                data[offset + 3] = 255;
            }

            using var pixelImage = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(data, image.Width, image.Height);
            using FileStream fs = new(path, FileMode.CreateNew);
            pixelImage.SaveAsPng(fs, null);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

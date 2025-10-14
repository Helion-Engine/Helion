using Helion.Graphics;
using System;
using System.IO;
namespace Helion.Util;

public interface IScreenshotGenerator
{
    Image? GetImage();
    void GeneratePngImage(Image image, Stream stream);
}

using Helion.Graphics;
using Helion.Util;
using System.IO;

namespace Helion.Tests.Unit.GameLayer;

public class MockScreenshotGenerator : IScreenshotGenerator
{
    public Image? GetImage() => new((1, 1), ImageType.Argb);
    public void GeneratePngImage(Image image, Stream stream)
    {
    }
}

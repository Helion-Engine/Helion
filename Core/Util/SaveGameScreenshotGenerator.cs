using Helion.Geometry;
using Helion.Graphics;
using Helion.Render;
using System;
using System.IO;

namespace Helion.Util;

public class SaveGameScreenshotGenerator(Renderer renderer) : IScreenshotGenerator
{
    private readonly Renderer m_renderer = renderer;

    public Image GetImage() => m_renderer.GetScreenshotFrameBufferData();

    public void GeneratePngImage(Image image, Stream stream)
    {
        image.SavePng(stream, null);
    }
}

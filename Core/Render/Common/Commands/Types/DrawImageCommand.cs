using System.Runtime.InteropServices;
using Helion.Graphics;
using Helion.Graphics.Geometry;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawImageCommand(string textureName, ImageBox2I drawArea, Color multiplyColor,
    float alpha = 1.0f, bool drawColorMap = false, bool drawFuzz = false, bool drawPalette = true)
{
    public readonly ImageBox2I DrawArea = drawArea;
    public readonly float Alpha = alpha;
    public readonly Color MultiplyColor = multiplyColor;
    public readonly bool AreaIsTextureDimension = false;
    public readonly bool DrawColorMap = drawColorMap;
    public readonly bool DrawFuzz = drawFuzz;
    public readonly bool DrawPalette = drawPalette;
    public readonly string TextureName = textureName;
}

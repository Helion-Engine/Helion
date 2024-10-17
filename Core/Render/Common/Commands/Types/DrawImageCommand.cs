using System.Runtime.InteropServices;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Resources;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawImageCommand(string textureName, ResourceNamespace ns, ImageBox2I drawArea, Color multiplyColor,
    float alpha = 1.0f, bool drawColorMap = false, bool drawFuzz = false, bool drawPalette = true, int colorMapIndex = 0)
{
    public readonly ImageBox2I DrawArea = drawArea;
    public readonly float Alpha = alpha;
    public readonly Color MultiplyColor = multiplyColor;
    public readonly bool AreaIsTextureDimension = false;
    public readonly bool DrawColorMap = drawColorMap;
    public readonly bool DrawFuzz = drawFuzz;
    public readonly bool DrawPalette = drawPalette;
    public readonly string TextureName = textureName;
    public readonly ResourceNamespace ResourceNamespace = ns;
    public readonly int ColorMapIndex = colorMapIndex;
}

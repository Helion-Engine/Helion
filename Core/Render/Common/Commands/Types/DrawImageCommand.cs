using System.Runtime.InteropServices;
using Helion.Graphics;
using Helion.Graphics.Geometry;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawImageCommand
{
    public readonly ImageBox2I DrawArea;
    public readonly float Alpha;
    public readonly Color MultiplyColor;
    public readonly bool AreaIsTextureDimension;
    public readonly bool DrawInvulnerability;
    public readonly bool DrawFuzz;
    public readonly bool DrawColorMap;
    public readonly string TextureName;

    public DrawImageCommand(string textureName, ImageBox2I drawArea, Color multiplyColor,
        float alpha = 1.0f, bool drawInvul = false, bool drawFuzz = false, bool drawColorMap = true)
    {
        TextureName = textureName;
        DrawArea = drawArea;
        Alpha = alpha;
        MultiplyColor = multiplyColor;
        AreaIsTextureDimension = false;
        DrawInvulnerability = drawInvul;
        DrawFuzz = drawFuzz;
        DrawColorMap = drawColorMap;
    }
}

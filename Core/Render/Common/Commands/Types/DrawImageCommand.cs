using System.Drawing;
using System.Runtime.InteropServices;
using Helion;
using Helion.Graphics.Geometry;
using Helion.Render;
using Helion.Render.Common.Commands.Types;

namespace Helion.Render.Common.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawImageCommand
{
    public readonly ImageBox2I DrawArea;
    public readonly float Alpha;
    public readonly Color MultiplyColor;
    public readonly bool AreaIsTextureDimension;
    public readonly bool DrawInvulnerability;
    public readonly string TextureName;

    public DrawImageCommand(string textureName, ImageBox2I drawArea, Color multiplyColor,
        float alpha = 1.0f, bool drawInvul = false)
    {
        TextureName = textureName;
        DrawArea = drawArea;
        Alpha = alpha;
        MultiplyColor = multiplyColor;
        AreaIsTextureDimension = false;
        DrawInvulnerability = drawInvul;
    }
}

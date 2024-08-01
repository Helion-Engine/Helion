using Helion.Graphics.Geometry;
using Helion.Render.OpenGL.Texture.Fonts;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawTextCommand
{
    public readonly ImageBox2I DrawArea;
    public readonly float Alpha;
    public readonly RenderableString Text;
    public readonly bool DrawColorMap;

    public DrawTextCommand(RenderableString text, ImageBox2I drawArea, float alpha, bool drawColorMap)
    {
        Text = text;
        DrawArea = drawArea;
        Alpha = alpha;
        DrawColorMap = drawColorMap;
    }

    public override string ToString() => Text.ToString();
}

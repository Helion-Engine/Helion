using Helion;
using Helion.Graphics.Geometry;
using Helion.Render;
using Helion.Render.Common.Commands.Types;
using Helion.Render.OpenGL.Texture.Fonts;
using System.Runtime.InteropServices;

namespace Helion.Render.Common.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawTextCommand
{
    public readonly ImageBox2I DrawArea;
    public readonly float Alpha;
    public readonly RenderableString Text;

    public DrawTextCommand(RenderableString text, ImageBox2I drawArea, float alpha)
    {
        Text = text;
        DrawArea = drawArea;
        Alpha = alpha;
    }

    public override string ToString() => Text.ToString();
}

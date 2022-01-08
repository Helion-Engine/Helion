using Helion.Graphics.Geometry;
using Helion.Render.Legacy.Texture.Fonts;
using System.Runtime.InteropServices;

namespace Helion.Render.Legacy.Commands.Types;

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

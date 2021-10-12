using Helion.Graphics.Geometry;
using Helion.Render.Legacy.Texture.Fonts;

namespace Helion.Render.Legacy.Commands.Types;

public record DrawTextCommand : IRenderCommand
{
    public readonly RenderableString Text;
    public readonly ImageBox2I DrawArea;
    public readonly float Alpha;

    public DrawTextCommand(RenderableString text, ImageBox2I drawArea, float alpha)
    {
        Text = text;
        DrawArea = drawArea;
        Alpha = alpha;
    }

    public override string ToString() => Text.ToString();
}

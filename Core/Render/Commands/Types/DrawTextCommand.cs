using Helion.Graphics.Fonts.Renderable;
using Helion.Graphics.Geometry;

namespace Helion.Render.Commands.Types
{
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
}
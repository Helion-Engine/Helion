using System.Drawing;
using Helion.Graphics.Fonts.Renderable;

namespace Helion.Render.Commands.Types
{
    public record DrawTextCommand : IRenderCommand
    {
        public readonly RenderableString Text;
        public readonly Rectangle DrawArea;
        public readonly float Alpha;

        public DrawTextCommand(RenderableString text, int x, int y, int width, int height, float alpha)
        {
            Text = text;
            DrawArea = new Rectangle(x, y, width, height);
            Alpha = alpha;
        }
    }
}
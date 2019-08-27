using System.Drawing;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawShapeCommand : IRenderCommand
    {
        public readonly Rectangle Rectangle;
        public readonly Color Color;
        public readonly float Alpha;

        public DrawShapeCommand(Rectangle rectangle, Color color, float alpha)
        {
            Rectangle = rectangle;
            Color = color;
            Alpha = alpha;
        }
    }
}
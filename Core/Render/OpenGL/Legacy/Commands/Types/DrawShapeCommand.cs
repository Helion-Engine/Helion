using System.Drawing;
using Helion.Graphics.Geometry;

namespace Helion.Render.OpenGL.Legacy.Commands.Types
{
    public record DrawShapeCommand : IRenderCommand
    {
        public readonly ImageBox2I Rectangle;
        public readonly Color Color;
        public readonly float Alpha;

        public DrawShapeCommand(ImageBox2I rectangle, Color color, float alpha)
        {
            Rectangle = rectangle;
            Color = color;
            Alpha = alpha;
        }
    }
}
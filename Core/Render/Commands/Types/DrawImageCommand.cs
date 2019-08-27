using System.Drawing;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawImageCommand : IRenderCommand
    {
        public readonly CIString TextureName;
        public readonly Rectangle DrawArea;
        public readonly float Alpha;
        public readonly Color Color;
        public readonly bool AreaIsTextureDimension;

        public DrawImageCommand(CIString textureName, Vec2I topLeft, float alpha = 1.0f) :
            this(textureName, topLeft, Color.Transparent, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, float alpha = 1.0f) :
            this(textureName, drawArea, Color.Transparent, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Vec2I topLeft, Color color, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = new Rectangle(topLeft.X, topLeft.Y, 16, 16);
            Alpha = alpha;
            Color = color;
            AreaIsTextureDimension = true;
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, Color color, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            Color = color;
            AreaIsTextureDimension = false;
        }
    }
}
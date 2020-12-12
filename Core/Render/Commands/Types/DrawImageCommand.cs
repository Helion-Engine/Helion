using System.Drawing;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawImageCommand : IRenderCommand
    {
        public readonly CIString TextureName;
        public readonly Rectangle DrawArea;
        public readonly float Alpha;
        public readonly Color MixColor;
        public readonly Color MultiplyColor;
        public readonly bool AreaIsTextureDimension;

        public DrawImageCommand(CIString textureName, Vec2I topLeft, float alpha = 1.0f) :
            this(textureName, topLeft, Color.Transparent, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, float alpha = 1.0f) :
            this(textureName, drawArea, Color.Transparent, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Vec2I topLeft, Color mixColor, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = new Rectangle(topLeft.X, topLeft.Y, 16, 16);
            Alpha = alpha;
            MixColor = mixColor;
            MultiplyColor = Color.Transparent;
            AreaIsTextureDimension = true;
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, Color mixColor, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            MixColor = mixColor;
            MultiplyColor = Color.Transparent;
            AreaIsTextureDimension = false;
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, Color mixColor, Color multiplyColor, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            MixColor = mixColor;
            MultiplyColor = multiplyColor;
            AreaIsTextureDimension = false;
        }
    }
}
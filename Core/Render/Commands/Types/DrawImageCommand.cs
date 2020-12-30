using System.Drawing;
using Helion.Graphics.Geometry;
using Helion.Util;

namespace Helion.Render.Commands.Types
{
    public record DrawImageCommand : IRenderCommand
    {
        public readonly CIString TextureName;
        public readonly ImageBox2I DrawArea;
        public readonly float Alpha;
        public readonly Color MultiplyColor;
        public readonly bool AreaIsTextureDimension;

        public DrawImageCommand(CIString textureName, int x, int y, float alpha = 1.0f) :
            this(textureName, new ImageBox2I(x, y, int.MaxValue, int.MaxValue), Color.White, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, ImageBox2I drawArea, float alpha = 1.0f) :
            this(textureName, drawArea, Color.White, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, ImageBox2I drawArea, Color multiplyColor, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            MultiplyColor = multiplyColor;
            AreaIsTextureDimension = false;
        }
    }
}
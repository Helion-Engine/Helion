using System.Drawing;
using Helion.Util;

namespace Helion.Render.Commands.Types
{
    public record DrawImageCommand : IRenderCommand
    {
        public readonly CIString TextureName;
        public readonly Rectangle DrawArea;
        public readonly float Alpha;
        public readonly Color MultiplyColor;
        public readonly bool AreaIsTextureDimension;

        public DrawImageCommand(CIString textureName, int x, int y, float alpha = 1.0f) :
            this(textureName, new Rectangle(x, y, int.MaxValue, int.MaxValue), Color.White, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, float alpha = 1.0f) :
            this(textureName, drawArea, Color.White, alpha)
        {
        }

        public DrawImageCommand(CIString textureName, Rectangle drawArea, Color multiplyColor, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            MultiplyColor = multiplyColor;
            AreaIsTextureDimension = false;
        }
    }
}
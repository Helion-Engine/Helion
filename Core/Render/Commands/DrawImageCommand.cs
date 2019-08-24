using System.Drawing;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.Commands
{
    public readonly struct DrawImageCommand : IRenderCommand
    {
        public readonly CIString TextureName;
        public readonly Rectangle DrawArea;
        public readonly float Alpha;
        public readonly bool AreaIsTextureDimension;

        public DrawImageCommand(CIString textureName, Rectangle drawArea, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            AreaIsTextureDimension = false;
        }
        
        public DrawImageCommand(CIString textureName, Vec2I topLeft, float alpha = 1.0f)
        {
            TextureName = textureName;
            DrawArea = new Rectangle(topLeft.X, topLeft.Y, 16, 16);
            Alpha = alpha;
            AreaIsTextureDimension = true;
        }
    }
}
using System.Drawing;
using Helion.Graphics.Geometry;
using Helion.Util;

namespace Helion.Render.Commands.Types
{
    public record DrawImageCommand : IRenderCommand
    {
        public readonly string TextureName;
        public readonly ImageBox2I DrawArea;
        public readonly float Alpha;
        public readonly Color MultiplyColor;
        public readonly bool AreaIsTextureDimension;
        public readonly bool DrawInvulnerability;

        public DrawImageCommand(string textureName, ImageBox2I drawArea, Color multiplyColor,
            float alpha = 1.0f, bool drawInvul = false)
        {
            TextureName = textureName;
            DrawArea = drawArea;
            Alpha = alpha;
            MultiplyColor = multiplyColor;
            AreaIsTextureDimension = false;
            DrawInvulnerability = drawInvul;
        }
    }
}
using Helion.Graphics.String;
using Helion.Render.Commands.Align;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.Commands.Types
{
    public record DrawTextCommand : IRenderCommand
    {
        public readonly ColoredString Text;
        public readonly string FontName;
        public readonly int FontSize;
        public readonly Vec2I Location;
        public readonly Dimension Dimension;
        public readonly TextAlignment TextAlignment;
        public readonly float Alpha;

        public DrawTextCommand(ColoredString text, string font, int fontSize, int x, int y, int width, int height,
            TextAlignment textAlign, float alpha)
        {
            Text = text;
            FontName = font;
            FontSize = fontSize;
            Location = new Vec2I(x, y);
            Dimension = new Dimension(width, height);
            TextAlignment = textAlign;
            Alpha = alpha;
        }
    }
}
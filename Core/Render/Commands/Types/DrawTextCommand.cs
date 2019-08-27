using Helion.Graphics.String;
using Helion.Util.Geometry;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawTextCommand : IRenderCommand
    {
        public readonly ColoredString Text;
        public readonly string FontName;
        public readonly Vec2I Location;
        public readonly float Alpha;
        public readonly int? FontSize;

        public DrawTextCommand(ColoredString text, string fontName, Vec2I location, float alpha, int? fontSize)
        {
            Text = text;
            FontName = fontName;
            Location = location;
            Alpha = alpha;
            FontSize = fontSize;
        }
    }
}
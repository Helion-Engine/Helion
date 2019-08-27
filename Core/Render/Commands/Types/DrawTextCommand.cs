using System.Drawing;
using Helion.Util.Geometry;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawTextCommand : IRenderCommand
    {
        public readonly string Text;
        public readonly string FontName;
        public readonly Vec2I Location;
        public readonly float Alpha;
        public readonly Color MixColor;
        public readonly Color MultiplyColor;
        public readonly int? FontSize;

        public DrawTextCommand(string text, string fontName, Vec2I location, float alpha, Color multiplyColor, int? fontSize)
        {
            Text = text;
            FontName = fontName;
            Location = location;
            Alpha = alpha;
            MixColor = Color.Transparent;
            MultiplyColor = multiplyColor;
            FontSize = fontSize;
        }
    }
}
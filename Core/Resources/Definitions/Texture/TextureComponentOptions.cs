using System;
using System.Drawing;

namespace Helion.Resources.Definitions.Texture
{
    [Flags]
    public enum TextureComponentFlags
    {
        None = 0,
        FlipX = 1,
        FlipY = 4,
        UseOffets = 8
    }

    public enum TextureComponentTranslation
    {
        None,
        Desaturate,
        Blue,
        Gold,
        Green,
        Ice,
        Inverse,
        Red
    }

    public enum TextureComponentStyle
    {
        None,
        Add,
        Copy,
        CopyAlpha,
        CopyNewAlpha,
        Modulate,
        Overlay,
        ReverseSubtract,
        Subtract,
        Translucent
    }

    public class TextureComponentOptions
    {
        public static readonly TextureComponentOptions Default = new();

        public double Rotate { get; set; }
        public TextureComponentTranslation Translation { get; set; }
        public double? TranslationAmount { get; set; }
        public double Alpha { get; set; }
        public double? BlendAlpha { get; set; }
        public Color? BlendColor { get; set; }
        public TextureComponentStyle Style { get; set; }
        public TextureComponentFlags Flags { get; set; }
    }
}

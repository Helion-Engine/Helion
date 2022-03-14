using Helion.Geometry.Vectors;
using System;

namespace Helion.Resources.Definitions.Texture
{
    [Flags]
    public enum TextureOptionFlags
    {
        None = 0,
        WorldPanning = 1,
        NoDecals = 2,
        NullTexture = 4,
        NoTrim = 8,
    }

    public class TextureOptions
    {
        public static readonly TextureOptions Default = new();

        public Vec2D Scale { get; set; }
        public Vec2D Offset { get; set; }
        public TextureOptionFlags Flags { get; set; }
    }
}

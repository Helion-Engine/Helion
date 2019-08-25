using System.Collections.Generic;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Fonts
{
    public class GLFontMetrics
    {
        private readonly GLGlyph DefaultGlyph;
        private readonly List<GLGlyph> Glyphs;

        public GLFontMetrics(GLGlyph defaultGlyph, List<GLGlyph> glyphs)
        {
            Precondition(!glyphs.Empty(), "Trying to make a GL font metrics object without any glyphs");

            DefaultGlyph = defaultGlyph;
            Glyphs = glyphs;
        }

        public GLGlyph this[char c] => c < Glyphs.Count ? Glyphs[c] : DefaultGlyph;
    }
}
// using System.Collections.Generic;
// using Helion.Util.Extensions;
// using static Helion.Util.Assertion.Assert;
//
// namespace Helion.Render.OpenGL.Texture.Fonts
// {
//     public class GLFontMetrics
//     {
//         public readonly int MaxHeight;
//         private readonly GLGlyph DefaultGlyph;
//         private readonly List<GLGlyph> Glyphs;
//
//         public GLFontMetrics(GLGlyph defaultGlyph, List<GLGlyph> glyphs, int maxHeight)
//         {
//             Precondition(!glyphs.Empty(), "Trying to make a GL font metrics object without any glyphs");
//             Precondition(maxHeight > 0, "Max height of a GL font cannot be zero or negative");
//
//             DefaultGlyph = defaultGlyph;
//             Glyphs = glyphs;
//             MaxHeight = maxHeight;
//         }
//
//         public GLGlyph this[char c] => c < Glyphs.Count ? Glyphs[c] : DefaultGlyph;
//     }
// }
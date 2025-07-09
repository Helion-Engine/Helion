using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Util.Container;

namespace Helion.Render.OpenGL.Texture.Fonts;

/// <summary>
/// A single sentence. We define a sentence as a single line of horizontal
/// characters, meaning it's not an actual sentence ended by a period, but
/// rather a single line of characters.
/// </summary>
public readonly struct RenderableSentence(DynamicArray<RenderableGlyph> glyphs, Dimension drawArea, Vec2I offset = default)
{
    /// <summary>
    /// The enclosing box around all the glyphs.
    /// </summary>
    public readonly Dimension DrawArea = drawArea;

    /// <summary>
    /// The glyphs and their draw positions.
    /// </summary>
    public readonly DynamicArray<RenderableGlyph> Glyphs = glyphs;

    public readonly Vec2I Offset = offset;

    public override string ToString()
    {
        char[] characters = new char[Glyphs.Length];
        for (int i = 0; i < Glyphs.Length; i++)
            characters[i] = Glyphs[i].Character;

        return new string(characters);
    }
}

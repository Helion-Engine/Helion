using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Util.Container;

namespace Helion.Render.OpenGL.Texture.Fonts;

/// <summary>
/// A single sentence. We define a sentence as a single line of horizontal
/// characters, meaning it's not an actual sentence ended by a period, but
/// rather a single line of characters.
/// </summary>
public struct RenderableSentence
{
    /// <summary>
    /// The enclosing box around all the glyphs.
    /// </summary>
    public readonly Dimension DrawArea;

    /// <summary>
    /// The glyphs and their draw positions.
    /// </summary>
    public readonly DynamicArray<RenderableGlyph> Glyphs;

    private char[] m_characters = new char[128];

    public RenderableSentence(DynamicArray<RenderableGlyph> glyphs)
    {
        Glyphs = glyphs;
        DrawArea = CalculateDrawArea(glyphs);
    }

    private static Dimension CalculateDrawArea(DynamicArray<RenderableGlyph> glyphs)
    {
        int width = 0;
        int height = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            var glyph = glyphs[i];
            if (glyph.Coordinates.Right > width)
                width = glyph.Coordinates.Right;
            if (glyph.Coordinates.Height > height)
                height = glyph.Coordinates.Height;
        }

        return new Dimension(width, height);
    }

    public override string ToString()
    {
        char[] characters = new char[Glyphs.Length];
        for (int i = 0; i < Glyphs.Length; i++)
            characters[i] = Glyphs[i].Character;

        return new string(characters);
    }
}

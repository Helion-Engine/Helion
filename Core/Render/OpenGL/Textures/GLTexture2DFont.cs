using Helion;
using Helion.Geometry.Boxes;
using Helion.Graphics.Fonts;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using zdbspSharp;

namespace Helion.Render.OpenGL.Textures;

public readonly record struct GLFontGlyph(char Char, Box2F UV);

public class GLTexture2DFont : GLTexture2D
{
    public readonly Font Font;
    private readonly Dictionary<char, Box2F> m_glyphs = new();
    private readonly Box2F m_defaultGlyphBox = Box2F.UnitBox;

    public int Height => Font.MaxHeight;

    public GLTexture2DFont(string label, Font font) : base(label, font.Image, TextureWrapMode.Clamp)
    {
        Font = font;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, $"Texture (font): {Label}");
        Unbind();

        foreach ((char c, Glyph g) in font)
            m_glyphs[c] = g.UV;
    }

    public Box2F this[char c] => m_glyphs.TryGetValue(c, out Box2F box) ? box : m_defaultGlyphBox;
}

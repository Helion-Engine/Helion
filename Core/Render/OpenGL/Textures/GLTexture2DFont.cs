using Helion;
using Helion.Graphics.Fonts;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures;

public class GLTexture2DFont : GLTexture2D
{
    public readonly Font Font;

    public int Height => Font.MaxHeight;

    public GLTexture2DFont(string label, Font font) : base(label, font.Image, TextureWrapMode.Clamp)
    {
        Font = font;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, $"Texture (font): {Label}");
        Unbind();

        // TODO: Populate glyph coordinates.
    }
}

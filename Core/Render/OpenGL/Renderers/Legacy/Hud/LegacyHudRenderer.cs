using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class LegacyHudRenderer : HudRenderer
{
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly VertexArrayObject m_vao;
    private readonly StreamVertexBuffer<HudVertex> m_vbo;
    private readonly LegacyHudShader m_program;
    private readonly HudDrawBuffer m_drawBuffer;
    private readonly IConfig m_config;
    private float DrawDepth = 1.0f;

    public LegacyHudRenderer(IConfig config, LegacyGLTextureManager textureManager, DataCache dataCache)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_vao = new("Hud renderer");
        m_vbo = new("Hud renderer");
        m_program = new();
        m_drawBuffer = new(dataCache);

        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
    }

    ~LegacyHudRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public override void Clear()
    {
        DrawDepth = 1.0f;
        m_vbo.Clear();
        m_drawBuffer.Clear();
    }

    public override void DrawImage(string textureName, ImageBox2I drawArea, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette)
    {
        m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
        AddImage(texture, drawArea, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);
    }

    public override void DrawImage(string textureName, Vec2I topLeft, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette)
    {
        m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
        (int width, int height) = texture.Dimension;
        ImageBox2I drawArea = new ImageBox2I(topLeft.X, topLeft.Y, topLeft.X + width, topLeft.Y + height);
        AddImage(texture, drawArea, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);
    }

    public override void DrawShape(ImageBox2I drawArea, Color color, float alpha)
    {
        GLLegacyTexture texture = m_textureManager.WhiteTexture;
        AddImage(texture, drawArea, (255, color.R, color.G, color.B), alpha, false, false, false);
    }

    public override void DrawText(RenderableString text, ImageBox2I drawArea, float alpha, bool drawPalette)
    {
        GLFontTexture<GLLegacyTexture> font = m_textureManager.GetFont(text.Font.Name);

        for (int i = 0; i < text.Sentences.Count; i++)
        {
            for (int j = 0; j < text.Sentences[i].Glyphs.Length; j++)
            {
                RenderableGlyph glyph = text.Sentences[i].Glyphs[j];
                float left = drawArea.Left + (float)(glyph.Location.Left * drawArea.Width);
                float top = drawArea.Top + (float)(glyph.Location.Top * drawArea.Height);
                float right = drawArea.Left + (float)(glyph.Location.Right * drawArea.Width);
                float bottom = drawArea.Top + (float)(glyph.Location.Bottom * drawArea.Height);
                float uvLeft = (float)glyph.UV.Left;
                float uvTop = (float)glyph.UV.Top;
                float uvRight = (float)glyph.UV.Right;
                float uvBottom = (float)glyph.UV.Bottom;

                HudVertex topLeft = MakeVertex(left, top, uvLeft, uvTop, glyph, alpha, drawPalette);
                HudVertex topRight = MakeVertex(right, top, uvRight, uvTop, glyph, alpha, drawPalette);
                HudVertex bottomLeft = MakeVertex(left, bottom, uvLeft, uvBottom, glyph, alpha, drawPalette);
                HudVertex bottomRight = MakeVertex(right, bottom, uvRight, uvBottom, glyph, alpha, drawPalette);

                HudQuad quad = new(topLeft, topRight, bottomLeft, bottomRight);
                m_drawBuffer.Add(font.Texture, quad);

                DrawDepth += 1.0f;
            }
        }
    }

    private HudVertex MakeVertex(float x, float y, float u, float v, RenderableGlyph glyph, float alpha, bool drawPalette)
    {
        return new(x, y, DrawDepth, u, v, glyph.Color, alpha, false, false, drawPalette);
    }

    public override void Render(Rectangle viewport, ShaderUniforms uniforms)
    {
        m_program.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.ColormapTexture(TextureUnit.Texture2);
        m_program.Mvp(CreateMvp(viewport));
        m_program.FuzzFrac(Renderer.GetTimeFrac());
        m_program.FuzzDiv(Renderer.GetFuzzDiv(m_config.Render, viewport));
        m_program.PaletteIndex((int)uniforms.PaletteIndex);
        m_program.ColorMapIndex(uniforms.ColorMapIndex);
        m_program.HasInvulnerability(uniforms.DrawInvulnerability);

        for (int i = 0; i < m_drawBuffer.DrawBuffer.Count; i++)
        {
            HudDrawBufferData data = m_drawBuffer.DrawBuffer[i];
            UploadVerticesToVbo(data);

            data.Texture.Bind();
            m_vao.Bind();
            m_vbo.DrawArrays();
            m_vao.Unbind();
            data.Texture.Unbind();
        }

        m_program.Unbind();
    }

    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void UploadVerticesToVbo(HudDrawBufferData data)
    {
        Precondition(!data.Vertices.Empty(), "Should have at least some vertices to draw for some hud texture");

        m_vbo.Clear();

        m_vbo.Bind();

        List<HudVertex> vertices = data.Vertices;
        for (int i = 0; i < vertices.Count; i++)
            m_vbo.Add(vertices[i]);

        m_vbo.Upload();
        m_vbo.Unbind();
    }

    private void AddImage(GLLegacyTexture texture, ImageBox2I drawArea, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette)
    {
        // Remember that we are drawing along the Z for visual depth now.
        var topLeft = new HudVertex(drawArea.Left, drawArea.Top, DrawDepth, 0.0f, 0.0f, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);
        var topRight = new HudVertex(drawArea.Right, drawArea.Top, DrawDepth, 1.0f, 0.0f, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);
        var bottomLeft = new HudVertex(drawArea.Left, drawArea.Bottom, DrawDepth, 0.0f, 1.0f, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);
        var bottomRight = new HudVertex(drawArea.Right, drawArea.Bottom, DrawDepth, 1.0f, 1.0f, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette);

        var quad = new HudQuad(topLeft, topRight, bottomLeft, bottomRight);
        m_drawBuffer.Add(texture, quad);

        // It is okay if there is a truncation here, we don't need exact
        // values, just enough to be able to distinguish between one image
        // to the next one after.
        DrawDepth += 1.0f;
    }

    private mat4 CreateMvp(Rectangle viewport)
    {
        // There's a few things we do here:
        //
        // 1) We draw from the top downwards because we have the top left
        // being our draw origin, and thus they are inverted.
        //
        // 2) We flip the Z depths so that we draw back-to-front, meaning
        // the stuff we drew first should be drawn behind the stuff we drew
        // later on. This gives us the Painters Algorithm approach we want.
        return mat4.Ortho(viewport.Left, viewport.Right, viewport.Bottom, viewport.Top, -(DrawDepth + 1), DrawDepth + 1);
    }

    private void ReleaseUnmanagedResources()
    {
        m_vao.Dispose();
        m_vbo.Dispose();
        m_program.Dispose();
    }
}

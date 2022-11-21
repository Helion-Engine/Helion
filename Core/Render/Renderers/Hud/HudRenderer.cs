using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GlmSharp;
using Helion;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Render;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Renderers.Hud;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Renderers.Hud;

public class HudRenderer : IDisposable
{
    private static readonly VertexArrayAttributes Attributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3),
        new VertexPointerFloatAttribute("uv", 1, 2),
        new VertexPointerUnsignedByteAttribute("rgbMultiplier", 3, 4, true),
        new VertexPointerFloatAttribute("alpha", 4, 1),
        new VertexPointerFloatAttribute("hasInvulnerability", 5, 1));

    private readonly IGLFunctions gl;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly VertexArrayObject m_vao;
    private readonly StreamVertexBuffer<HudVertex> m_vbo;
    private readonly HudShader m_shaderProgram;
    private readonly HudDrawBuffer m_drawBuffer;
    private float DrawDepth = 1.0f;

    public HudRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager, DataCache dataCache)
    {
        gl = functions;
        m_textureManager = textureManager;
        m_vao = new VertexArrayObject(capabilities, functions, Attributes, "VAO: Hud renderer");
        m_vbo = new StreamVertexBuffer<HudVertex>(capabilities, functions, m_vao, "VBO: Hud renderer");
        m_drawBuffer = new(dataCache);

        using (ShaderBuilder shaderBuilder = HudShader.MakeBuilder(functions))
            m_shaderProgram = new HudShader(functions, shaderBuilder, Attributes);
    }

    ~HudRenderer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void Clear()
    {
        DrawDepth = 1.0f;
        m_vbo.Clear();
        m_drawBuffer.Clear();
    }

    public void DrawImage(string textureName, ImageBox2I drawArea, Color multiplyColor,
        float alpha, bool drawInvul)
    {
        m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
        AddImage(texture, drawArea, multiplyColor, alpha, drawInvul);
    }

    public void DrawImage(string textureName, Vec2I topLeft, Color multiplyColor,
        float alpha, bool drawInvul)
    {
        m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
        (int width, int height) = texture.Dimension;
        ImageBox2I drawArea = new ImageBox2I(topLeft.X, topLeft.Y, topLeft.X + width, topLeft.Y + height);
        AddImage(texture, drawArea, multiplyColor, alpha, drawInvul);
    }

    public void DrawShape(ImageBox2I drawArea, Color color, float alpha)
    {
        GLLegacyTexture texture = m_textureManager.WhiteTexture;
        AddImage(texture, drawArea, Color.FromArgb(255, color.R, color.G, color.B), alpha, false);
    }

    public void DrawText(RenderableString text, ImageBox2I drawArea, float alpha)
    {
        GLFontTexture<GLLegacyTexture> font = m_textureManager.GetFont(text.Font.Name);

        for (int i = 0; i < text.Sentences.Count; i++)
        {
            for (int j = 0; j < text.Sentences[i].Glyphs.Count; j++)
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

                HudVertex topLeft = MakeVertex(left, top, uvLeft, uvTop, glyph);
                HudVertex topRight = MakeVertex(right, top, uvRight, uvTop, glyph);
                HudVertex bottomLeft = MakeVertex(left, bottom, uvLeft, uvBottom, glyph);
                HudVertex bottomRight = MakeVertex(right, bottom, uvRight, uvBottom, glyph);

                HudQuad quad = new HudQuad(topLeft, topRight, bottomLeft, bottomRight);
                m_drawBuffer.Add(font.Texture, quad);
            }
        }

        DrawDepth += 1.0f;

        HudVertex MakeVertex(float x, float y, float u, float v, RenderableGlyph glyph)
        {
            return new(x, y, DrawDepth, u, v, glyph.Color, alpha, false);
        }
    }

    public void Render(Rectangle viewport)
    {
        m_shaderProgram.Bind();

        gl.ActiveTexture(TextureUnitType.Zero);
        m_shaderProgram.BoundTexture.Set(gl, 0);
        m_shaderProgram.Mvp.Set(gl, CreateMvp(viewport));

        // TODO: Bind VAO and VBO out here and not constantly bind/unbind?
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

        m_shaderProgram.Unbind();
    }

    public void Dispose()
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
        float alpha, bool drawInvul)
    {
        // Remember that we are drawing along the Z for visual depth now.
        HudVertex topLeft = new HudVertex(drawArea.Left, drawArea.Top, DrawDepth, 0.0f, 0.0f, multiplyColor, alpha, drawInvul);
        HudVertex topRight = new HudVertex(drawArea.Right, drawArea.Top, DrawDepth, 1.0f, 0.0f, multiplyColor, alpha, drawInvul);
        HudVertex bottomLeft = new HudVertex(drawArea.Left, drawArea.Bottom, DrawDepth, 0.0f, 1.0f, multiplyColor, alpha, drawInvul);
        HudVertex bottomRight = new HudVertex(drawArea.Right, drawArea.Bottom, DrawDepth, 1.0f, 1.0f, multiplyColor, alpha, drawInvul);

        HudQuad quad = new HudQuad(topLeft, topRight, bottomLeft, bottomRight);
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
        return mat4.Ortho(viewport.Left, viewport.Right, viewport.Bottom, viewport.Top, -(DrawDepth + 1), 0);
    }

    private void ReleaseUnmanagedResources()
    {
        m_vao.Dispose();
        m_vbo.Dispose();
        m_shaderProgram.Dispose();
    }
}

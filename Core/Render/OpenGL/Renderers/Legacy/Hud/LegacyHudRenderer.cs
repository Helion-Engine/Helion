using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Graphics.Fonts;
using Helion.Graphics.String;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class LegacyHudRenderer : HudRenderer
    {
        private static readonly VertexArrayAttributes Attributes = new(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2),
            new VertexPointerUnsignedByteAttribute("rgbMultiplier", 3, 4, true),
            new VertexPointerFloatAttribute("alpha", 4, 1));

        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly StreamVertexBuffer<HudVertex> m_vbo;
        private readonly LegacyHudShader m_shaderProgram;
        private readonly HudDrawBuffer m_drawBuffer = new();
        private float DrawDepth = 1.0f;

        public LegacyHudRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(capabilities, functions, Attributes, "VAO: Hud renderer");
            m_vbo = new StreamVertexBuffer<HudVertex>(capabilities, functions, m_vao, "VBO: Hud renderer");

            using (ShaderBuilder shaderBuilder = LegacyHudShader.MakeBuilder(functions))
                m_shaderProgram = new LegacyHudShader(functions, shaderBuilder, Attributes);
        }

        ~LegacyHudRenderer()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public override void Clear()
        {
            DrawDepth = 1.0f;
            m_vbo.Clear();
            m_drawBuffer.Clear();
        }

        public override void DrawImage(CIString textureName, Rectangle drawArea, Color multiplyColor,
            float alpha)
        {
            m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
            AddImage(texture, drawArea, multiplyColor, alpha);
        }

        public override void DrawImage(CIString textureName, Vec2I topLeft, Color multiplyColor,
            float alpha)
        {
            m_textureManager.TryGet(textureName, ResourceNamespace.Graphics, out GLLegacyTexture texture);
            Dimension dimension = texture.Dimension;
            Rectangle drawArea = new Rectangle(topLeft.X, topLeft.Y, dimension.Width, dimension.Height);
            AddImage(texture, drawArea, multiplyColor, alpha);
        }

        public override void DrawShape(Rectangle area, Color color, float alpha)
        {
            GLLegacyTexture texture = m_textureManager.WhiteTexture;
            AddImage(texture, area, Color.FromArgb(255, color.R, color.G, color.B), alpha);
        }

        public override void DrawText(ColoredString text, CIString fontName, int fontHeight, Vec2I topLeftDraw, float alpha)
        {
            GLFontTexture<GLLegacyTexture> font = m_textureManager.GetFont(fontName);
            float fontScale = (float)fontHeight / font.Height;

            float offset = topLeftDraw.X;
            foreach (ColoredChar c in text)
            {
                FontGlyph fontGlyph = font[c.Character];
                int glyphWidth = fontGlyph.Location.Width;

                // We truncate to an integer because this is how it is also
                // calculated from the text width/height calculators.
                float width = (int)(glyphWidth * fontScale);

                float top = topLeftDraw.Y;
                float bottom = top + fontHeight;
                float left = offset;
                float right = left + width;

                Box2D uv = fontGlyph.UV;
                float uvLeft = (float)uv.Left;
                float uvRight = (float)uv.Right;
                // We draw from the top down, so the V's have to flip.
                float uvTop = (float)uv.Bottom;
                float uvBottom = (float)uv.Top;

                HudVertex topLeft = new HudVertex(left, top, DrawDepth, uvLeft, uvTop, c.Color, alpha);
                HudVertex topRight = new HudVertex(right, top, DrawDepth, uvRight, uvTop, c.Color, alpha);
                HudVertex bottomLeft = new HudVertex(left, bottom, DrawDepth, uvLeft, uvBottom, c.Color, alpha);
                HudVertex bottomRight = new HudVertex(right, bottom, DrawDepth, uvRight, uvBottom, c.Color, alpha);
                HudQuad quad = new HudQuad(topLeft, topRight, bottomLeft, bottomRight);
                m_drawBuffer.Add(font.Texture, quad);

                offset += width;
            }

            DrawDepth += 1.0f;
        }

        public override void Render(Rectangle viewport)
        {
            m_shaderProgram.Bind();

            gl.ActiveTexture(TextureUnitType.Zero);
            m_shaderProgram.BoundTexture.Set(gl, 0);
            m_shaderProgram.Mvp.Set(gl, CreateMvp(viewport));

            // TODO: Bind VAO and VBO out here and not constantly bind/unbind?
            foreach (HudDrawBufferData data in m_drawBuffer.DrawBuffer)
            {
                UploadVerticesToVbo(data);

                data.Texture.Bind();
                m_vao.Bind();
                m_vbo.DrawArrays();
                m_vao.Unbind();
                data.Texture.Unbind();
            }

            m_shaderProgram.Unbind();
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

        private void AddImage(GLLegacyTexture texture, Rectangle drawArea, Color multiplyColor,
            float alpha)
        {
            // Remember that we are drawing along the Z for visual depth now.
            HudVertex topLeft = new HudVertex(drawArea.Left, drawArea.Top, DrawDepth, 0.0f, 0.0f, multiplyColor, alpha);
            HudVertex topRight = new HudVertex(drawArea.Right, drawArea.Top, DrawDepth, 1.0f, 0.0f, multiplyColor, alpha);
            HudVertex bottomLeft = new HudVertex(drawArea.Left, drawArea.Bottom, DrawDepth, 0.0f, 1.0f, multiplyColor, alpha);
            HudVertex bottomRight = new HudVertex(drawArea.Right, drawArea.Bottom, DrawDepth, 1.0f, 1.0f, multiplyColor, alpha);

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
}
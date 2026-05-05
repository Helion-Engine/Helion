using System;
using System.Runtime.CompilerServices;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Configs;
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

    public override void DrawImage(string textureName, ResourceNamespace ns, ImageBox2I drawArea, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette, int colorMapIndex, string? brightmapName = null, ImageBox2I? crop = null)
    {
        m_textureManager.TryGet(textureName, ns, out GLLegacyTexture texture);

        GLLegacyTexture? brightmapTexture = null;
        if (brightmapName != null && m_textureManager.TryGet(brightmapName, ResourceNamespace.Brightmaps, out GLLegacyTexture val))
            brightmapTexture = val;

        AddImage(texture, drawArea, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex, brightmapTexture, crop);
    }

    public override void DrawImage(string textureName, ResourceNamespace ns, Vec2I topLeft, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette, int colorMapIndex, string? brightmapName = null, ImageBox2I? crop = null)
    {
        m_textureManager.TryGet(textureName, ns, out GLLegacyTexture texture);
        
        int width = crop?.Width ?? texture.Dimension.Width;
        int height = crop?.Height ?? texture.Dimension.Height;
        
        ImageBox2I drawArea = new(topLeft.X, topLeft.Y, topLeft.X + width, topLeft.Y + height);

        GLLegacyTexture? brightmapTexture = null;
        if (brightmapName != null && m_textureManager.TryGet(brightmapName, ResourceNamespace.Brightmaps, out GLLegacyTexture val))
            brightmapTexture = val;

        AddImage(texture, drawArea, multiplyColor, alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex, brightmapTexture, crop);
    }

    public override void DrawShape(ImageBox2I drawArea, Color color, float alpha)
    {
        GLLegacyTexture texture = m_textureManager.WhiteTexture;
        AddImage(texture, drawArea, (255, color.R, color.G, color.B), alpha, false, false, false, 0);
    }

    public override void DrawText(RenderableString text, ImageBox2I drawArea, float alpha, bool drawPalette)
    {
        if (text.Sentences.Length == 0)
            return;

        var font = m_textureManager.GetFont(text.Font.Name);
        var drawAreaWidth = drawArea.Width;
        var drawAreaHeight = drawArea.Height;
        var drawAreaLeft = drawArea.Min.X;
        var drawAreaTop = drawArea.Min.Y;

        var hudDrawBuffer = m_drawBuffer.GetOrCreate(font.Texture).Vertices;
        int writeIndex = hudDrawBuffer.Length;

        for (int i = 0; i < text.Sentences.Length; i++)
        {
            ref var sentence = ref text.Sentences.Data[i];
            hudDrawBuffer.EnsureCapacity(writeIndex + (sentence.Glyphs.Length * 6));
            for (int j = 0; j < sentence.Glyphs.Length; j++)
            {
                ref var glyph = ref sentence.Glyphs.Data[j];
                float left = drawAreaLeft + (glyph.Location.Min.X * drawAreaWidth) + sentence.Offset.X;
                float top = drawAreaTop + (glyph.Location.Min.Y * drawAreaHeight) + sentence.Offset.Y;
                float right = drawAreaLeft + (glyph.Location.Max.X * drawAreaWidth) + sentence.Offset.X;
                float bottom = drawAreaTop + (glyph.Location.Max.Y * drawAreaHeight) + sentence.Offset.Y;
                float uvLeft = glyph.UV.Min.X;
                float uvTop = glyph.UV.Min.Y;
                float uvRight = glyph.UV.Max.X;
                float uvBottom = glyph.UV.Max.Y;

                var topLeft = MakeVertex(left, top, uvLeft, uvTop, glyph, alpha, drawPalette);
                var topRight = MakeVertex(right, top, uvRight, uvTop, glyph, alpha, drawPalette);
                var bottomLeft = MakeVertex(left, bottom, uvLeft, uvBottom, glyph, alpha, drawPalette);
                var bottomRight = MakeVertex(right, bottom, uvRight, uvBottom, glyph, alpha, drawPalette);

                hudDrawBuffer.Data[writeIndex++] = topLeft;
                hudDrawBuffer.Data[writeIndex++] = bottomLeft;
                hudDrawBuffer.Data[writeIndex++] = topRight;
                hudDrawBuffer.Data[writeIndex++] = topRight;
                hudDrawBuffer.Data[writeIndex++] = bottomLeft;
                hudDrawBuffer.Data[writeIndex++] = bottomRight;

                DrawDepth += 1.0f;
            }
        }

        hudDrawBuffer.Length = writeIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HudVertex MakeVertex(float x, float y, float u, float v, in RenderableGlyph glyph, float alpha, bool drawPalette)
    {
        return new(x, y, DrawDepth, u, v, glyph.Color.R, glyph.Color.G, glyph.Color.B, glyph.Color.A, alpha, false, false, drawPalette, 0);
    }

    public override void Render(Rectangle viewport, Dimension windowDimension, Dimension virtualDimension, ShaderUniforms uniforms)
    {
        m_program.Bind();

        GetFuzzSampleFactorAndOffset(windowDimension, virtualDimension, out var fuzzSampleFactor, out var fuzzSampleOffset);

        GL.ActiveTexture(BindTextures.BoundTexture);
        m_program.BoundTexture(BindTextures.BoundTexture);
        m_program.ColormapTexture(BindTextures.Colormap);
        m_program.OpaqueTexture(BindTextures.OpaqueTexture);
        m_program.BrightmapTexture(BindTextures.BrightmapTexture);
        m_program.Mvp(CreateMvp(viewport));
        m_program.FuzzFrac(Renderer.GetTimeFrac());
        m_program.FuzzDiv(Renderer.GetFuzzDiv(m_config.Render, viewport));
        m_program.FuzzRefraction(m_config.Render.PostProcessingEffects.Value);
        m_program.FuzzSampleFactor(fuzzSampleFactor);
        m_program.FuzzSampleOffset(fuzzSampleOffset);
        m_program.PaletteIndex((int)uniforms.PaletteIndex);
        m_program.ColorMapIndex(uniforms.ColorMapUniforms.SectorIndex == 0 ? uniforms.ColorMapUniforms.GlobalIndex : uniforms.ColorMapUniforms.SectorIndex);
        m_program.HasInvulnerability(uniforms.DrawInvulnerability);
        m_program.GammaCorrection(uniforms.GammaCorrection);
        m_program.ScreenBounds((viewport.Width, viewport.Height));

        for (int i = 0; i < m_drawBuffer.DrawBuffer.Count; i++)
        {
            HudDrawBufferData data = m_drawBuffer.DrawBuffer[i];
            UploadVerticesToVbo(data);

            GL.ActiveTexture(BindTextures.BoundTexture);
            data.Texture.Bind();
            GL.ActiveTexture(BindTextures.BrightmapTexture);
            if (data.BrightmapTexture != null)
                data.BrightmapTexture.Bind();
            else
                GL.BindTexture(TextureTarget.Texture2D, 0);
            m_vao.Bind();
            m_vbo.DrawArrays();
            m_vao.Unbind();
            data.Texture.Unbind();
        }

        m_program.Unbind();
    }

    private void GetFuzzSampleFactorAndOffset(Dimension windowDimension, Dimension virtualDimension, out Vec2F factor, out Vec2F offset)
    {
        if (m_config.Window.Virtual.Stretch || !m_config.Window.Virtual.Enable)
        {
            offset = Vec2F.Zero;
            factor = (virtualDimension.Width / (float)windowDimension.Width, virtualDimension.Height / (float)windowDimension.Height);
            return;
        }

        var mainWidth = windowDimension.Width;
        var mainHeight = windowDimension.Height;
        var virtualWidth = virtualDimension.Width;
        var virtualHeight = virtualDimension.Height;

        var scale = Math.Min(mainWidth / (float)virtualWidth, mainHeight / (float)virtualHeight);

        var displayWidth = virtualWidth * scale;
        var displayHeight = virtualHeight * scale;

        var offsetX = (mainWidth - displayWidth) * 0.5f;
        var offsetY = (mainHeight - displayHeight) * 0.5f;

        factor = (virtualWidth / displayWidth,  virtualHeight / displayHeight);
        offset = (-offsetX * factor.X, -offsetY * factor.Y);
    }

    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private unsafe void UploadVerticesToVbo(HudDrawBufferData data)
    {
        Precondition(data.Vertices.Length != 0, "Should have at least some vertices to draw for some hud texture");

        m_vbo.Clear();
        m_vbo.Bind();

        var vertices = data.Vertices;
        m_vbo.Data.EnsureCapacity(vertices.Length);

        fixed (HudVertex* pSrc = &vertices.Data[0])
        fixed (HudVertex* pDst = &m_vbo.Data.Data[0])
        {
            var bufferSize = vertices.Length * sizeof(HudVertex);
            System.Buffer.MemoryCopy(pSrc, pDst, bufferSize, bufferSize);
        }

        m_vbo.Data.Length = vertices.Length;

        m_vbo.Upload();
        m_vbo.Unbind();
    }

    private void AddImage(GLLegacyTexture texture, ImageBox2I drawArea, Color multiplyColor,
        float alpha, bool drawColorMap, bool drawFuzz, bool drawPalette, int colorMapIndex, 
        GLLegacyTexture? brightmapTexture = null, ImageBox2I? crop = null)
    {
        float u0 = 0.0f;
        float v0 = 0.0f;
        float u1 = 1.0f;
        float v1 = 1.0f;

        if (crop.HasValue)
        {
            var c = crop.Value;
            
            u0 = c.Min.X / (float)texture.Dimension.Width;
            v0 = c.Min.Y / (float)texture.Dimension.Height;
            u1 = c.Max.X / (float)texture.Dimension.Width;
            v1 = c.Max.Y / (float)texture.Dimension.Height;
        }

        // Remember that we are drawing along the Z for visual depth now.
        var topLeft = new HudVertex(
            drawArea.Left, drawArea.Top, DrawDepth, 
            u0, v0, 
            multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A, 
            alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex);

        var topRight = new HudVertex(
            drawArea.Right, drawArea.Top, DrawDepth, 
            u1, v0, 
            multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A, 
            alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex);

        var bottomLeft = new HudVertex(
            drawArea.Left, drawArea.Bottom, DrawDepth, 
            u0, v1, 
            multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A, 
            alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex);

        var bottomRight = new HudVertex(
            drawArea.Right, drawArea.Bottom, DrawDepth, 
            u1, v1, 
            multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A, 
            alpha, drawColorMap, drawFuzz, drawPalette, colorMapIndex);
        
        var quad = new HudQuad(topLeft, topRight, bottomLeft, bottomRight);
        m_drawBuffer.Add(texture, quad, brightmapTexture);

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

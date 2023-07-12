using System;
using System.Diagnostics;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.RenderNew.OpenGL.Textures;
using Helion.RenderNew.Surfaces;
using Helion.RenderNew.Textures;
using Helion.Resources;
using Helion.Util.Configs;

namespace Helion.RenderNew.Renderers.Hud;

public class HudRenderingContext : IDisposable
{
    private readonly IConfig m_config;
    private readonly GLTextureManager m_textureManager;
    private readonly HudRenderer m_hudRenderer;
    private bool m_currentlyRendering;
    private bool m_disposed;

    public HudRenderingContext(IConfig config, GLTextureManager textureManager, HudRenderer hudRenderer)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_hudRenderer = hudRenderer;
    }

    public void Begin()
    {
        Debug.Assert(!m_disposed, $"Trying to begin HUD rendering on a disposed HUD rendering context");
        
        if (m_currentlyRendering)
            throw new($"Did not call {nameof(Begin)}() for {nameof(HudRenderingContext)}");

        m_currentlyRendering = true;
    }
    
    public void End()
    {
        if (!m_currentlyRendering)
            throw new($"Did not call {nameof(End)}() for {nameof(HudRenderingContext)}");
        
        FlushWork();
        
        m_currentlyRendering = false;
    }

    public void FlushWork()
    {
        // TODO
    }

    public void DrawLine(Seg2F seg, RgbColor color)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw lines when {nameof(Begin)} was not called");
        
        // TODO: Write to GL_LINES
    }
    
    public void DrawRect(Box2I box, RgbColor color)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw boxes when {nameof(Begin)} was not called");

        DrawLine((box.BottomLeft.Float, box.TopLeft.Float), color);
        DrawLine((box.TopLeft.Float, box.TopRight.Float), color);
        DrawLine((box.TopRight.Float, box.BottomRight.Float), color);
        DrawLine((box.BottomRight.Float, box.BottomLeft.Float), color);
    }
    
    public void FillRect(Box2I box, RgbColor color)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to fill boxes when {nameof(Begin)} was not called");

        DrawImage(m_textureManager.WhiteTexture, box, blend: color);
    }
    
    public void DrawImage(string textureName, Vec2I origin, float scale = 1.0f, RgbColor? blend = null, float alpha = 1.0f)
    {
        m_textureManager.Get(textureName, ResourceNamespace.Global, out GLTexture2D texture);
        Box2I box = (origin, origin + texture.Dimension);
        DrawImage(texture, box, scale, blend, alpha);
    }
    
    public void DrawImage(string textureName, Box2I box, float scale = 1.0f, RgbColor? blend = null, float alpha = 1.0f)
    {
        m_textureManager.Get(textureName, ResourceNamespace.Global, out GLTexture2D texture);
        DrawImage(texture, box, scale, blend, alpha);
    }

    private void DrawImage(GLTexture2D texture, Box2I box, float scale = 1.0f, RgbColor? blend = null, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw images when {nameof(Begin)} was not called");
        Debug.Assert(scale > 0.0f, "Scaling an image must be done with a positive value");
        Debug.Assert(alpha >= 0.0f, "Alpha must not be negative");
        
        if (alpha <= 0.0f)
            return;
        
        // TODO
    }

    public void DrawSurface(GLTextureSurface surface, Box2I box, float scale = 1.0f, RgbColor? blend = null, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw a surface when {nameof(Begin)} was not called");

        if (alpha <= 0.0f)
            return;
        
        // TODO
    }
    
    public void DrawText(string text, string font, int height, RgbColor color, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw text when {nameof(Begin)} was not called");
        Debug.Assert(height >= 0, "Font height must not be negative");

        if (height <= 0 || alpha <= 0.0f)
            return;

        // TODO
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        if (m_currentlyRendering)
            throw new($"Trying to dispose {nameof(HudRenderingContext)} while actively rendering");
        
        m_disposed = true;
        GC.SuppressFinalize(this);
    }
}
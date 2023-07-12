using System;
using System.Diagnostics;
using Helion.Geometry.Boxes;
using Helion.Geometry.Quads;
using Helion.Geometry.Segments;
using Helion.Graphics;
using Helion.RenderNew.OpenGL.Textures;
using Helion.RenderNew.Surfaces;
using Helion.RenderNew.Textures;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.World;

namespace Helion.RenderNew.Renderers.World;

public struct WorldRenderingInfo
{
    // Position
    // Angle
    // Re-use rendered data if not the first pass
}

public class WorldRenderingContext : IDisposable
{
    private readonly IConfig m_config;
    private readonly GLTextureManager m_textureManager;
    private readonly WorldRenderer m_worldRenderer;
    private bool m_currentlyRendering;
    private bool m_disposed;

    public WorldRenderingContext(IConfig config, GLTextureManager textureManager, WorldRenderer worldRenderer)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_worldRenderer = worldRenderer;
    }

    public void Begin()
    {
        Debug.Assert(!m_disposed, $"Trying to begin world rendering on a disposed world rendering context");
        
        if (m_currentlyRendering)
            throw new($"Did not call {nameof(Begin)}() for {nameof(WorldRenderingContext)}");

        m_currentlyRendering = true;
    }
    
    public void End()
    {
        if (!m_currentlyRendering)
            throw new($"Did not call {nameof(End)}() for {nameof(WorldRenderingContext)}");

        FlushWork();
        
        m_currentlyRendering = false;
    }

    public void FlushWork()
    {
        // TODO
    }

    public void DrawSegment(Seg3F seg, RgbColor color)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world segments when {nameof(Begin)} was not called");
        
        // TODO
    }
    
    public void DrawBox(Box3F box, RgbColor color)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world box when {nameof(Begin)} was not called");
        
        // TODO: Draw 12 edges as segments.
    }
    
    public void FillBox(Box3F box, RgbColor color, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to fill world box when {nameof(Begin)} was not called");
        
        // TODO: Fill 6 quads.
    }
    
    public void DrawQuad(Quad3D quad, RgbColor color, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world quad when {nameof(Begin)} was not called");
        
        // TODO: Draw 4 edges as segments.
    }
    
    public void FillQuad(Quad3D quad, RgbColor color, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to fill world quad when {nameof(Begin)} was not called");
        
        DrawTexture(m_textureManager.WhiteTexture, quad, blend: color, alpha: alpha);
    }
    
    public void DrawImage(string textureName, Quad3D quad, RgbColor? blend = null, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world image when {nameof(Begin)} was not called");
        
        m_textureManager.Get(textureName, ResourceNamespace.Global, out var texture);
        DrawTexture(texture, quad, blend, alpha);
    }
    
    public void DrawSurface(GLTextureSurface surface, Quad3D quad, RgbColor? blend = null, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world surface when {nameof(Begin)} was not called");
        
        // TODO: DrawTexture(...)
    }

    private void DrawTexture(GLTexture texture, Quad3D quad, RgbColor? blend = null, float alpha = 1.0f)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to draw world texture when {nameof(Begin)} was not called");
        
        // TODO
    }
    
    public void Render(IWorld world, in WorldRenderingInfo renderInfo)
    {
        Debug.Assert(m_currentlyRendering, $"Trying to render a world when {nameof(Begin)} was not called");
        
        // TODO
    }
    
    public void Dispose()
    {
        if (m_disposed)
            return;
        
        if (m_currentlyRendering)
            throw new($"Trying to dispose {nameof(WorldRenderingContext)} while actively rendering");
        
        m_disposed = true;
    }
}
using System;
using System.Diagnostics;
using Helion.RenderNew.Textures;
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
        
        // TODO: Flush renderer.
        
        m_currentlyRendering = false;
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
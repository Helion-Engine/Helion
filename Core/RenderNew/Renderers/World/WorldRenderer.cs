using System;
using Helion.RenderNew.Interfaces;
using Helion.RenderNew.Interfaces.World;
using Helion.RenderNew.Renderers.World.Geometry;
using Helion.RenderNew.Textures;
using Helion.Util.Configs;

namespace Helion.RenderNew.Renderers.World;

public class WorldRenderer : IDisposable
{
    public readonly WorldRenderingContext Context;
    private readonly WeakReference<IRenderableWorld?> m_lastRenderedWorld = new(null);
    private readonly WorldGeometryRenderer m_geometryRenderer = new();
    private bool m_disposed;

    public WorldRenderer(IConfig config, GLAtlasTextureManager textureManager)
    {
        Context = new(config, textureManager, this);
    }

    internal void Render(IRenderableWorld world, WorldRenderingInfo renderInfo)
    {
        if (!IsLastRenderedWorld(world))
            UpdateToWorld(world);

        m_geometryRenderer.Render(renderInfo);
    }

    private bool IsLastRenderedWorld(IRenderableWorld world)
    {
        return m_lastRenderedWorld.TryGetTarget(out IRenderableWorld? lastRenderedWorld) && ReferenceEquals(lastRenderedWorld, world);
    }

    private void UpdateToWorld(IRenderableWorld world)
    {
        m_geometryRenderer.UpdateTo(world);
    }

    private void ReleaseManagedResources()
    {
        if (m_disposed)
            return;
        
        m_geometryRenderer.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ReleaseManagedResources();
    }
}

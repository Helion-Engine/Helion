using System;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static;

public class StaticGeometryRenderer : IDisposable
{
    private readonly StaticWallRenderer m_wallRenderer;
    private bool m_disposed;

    public StaticGeometryRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
    {
        m_wallRenderer = new StaticWallRenderer(textureManager, textureDataBuffer);
    }

    ~StaticGeometryRenderer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void UpdateTo(IWorld world)
    {
        m_wallRenderer.UpdateTo(world);
    }

    public void Render(WorldRenderContext context)
    {
        m_wallRenderer.Render(context);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        m_wallRenderer.Dispose();

        m_disposed = true;
    }
}


using System;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Renderers.World.Geometry.Static;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry;

public class GeometryRenderer : IGLWorldRenderer
{
    private readonly StaticGeometryRenderer m_staticGeometry;
    private WeakReference<IWorld>? m_world;
    private bool m_disposed;

    public GeometryRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
    {
        m_staticGeometry = new StaticGeometryRenderer(textureManager, textureDataBuffer);
    }

    ~GeometryRenderer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    private bool IsNewOrDifferentWorld(IWorld world)
    {
        if (m_world == null)
            return true;

        return m_world != null &&
               m_world.TryGetTarget(out IWorld? oldWorld) &&
               ReferenceEquals(world, oldWorld);
    }

    public void Draw(IWorld world)
    {
        if (IsNewOrDifferentWorld(world))
        {
            m_world = new WeakReference<IWorld>(world);
            m_staticGeometry.UpdateTo(world);
        }
    }

    public void Render(WorldRenderContext context)
    {
        if (m_world == null)
            return;

        m_staticGeometry.Render(context);
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

        m_staticGeometry.Dispose();

        m_disposed = true;
    }
}


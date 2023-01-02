using System;
using Helion.Render.OpenGL.Shared;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers;

/// <summary>
/// Responsible for rendering a world.
/// </summary>
public abstract class WorldRenderer : IDisposable
{
    private readonly WeakReference<IWorld?> m_lastRenderedWorld = new WeakReference<IWorld?>(null);

    /// <summary>
    /// Performs rendering on the world provided with the information for
    /// rendering.
    /// </summary>
    /// <param name="world">The world to render.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    public void Render(IWorld world, RenderInfo renderInfo)
    {
        if (IsWorldNotSeenBefore(world))
        {
            m_lastRenderedWorld.SetTarget(world);
            UpdateToNewWorld(world);
        }

        if (renderInfo.DrawAutomap)
            PerformAutomapRender(world, renderInfo);
        else
            PerformRender(world, renderInfo);
    }

    public abstract void ResetInterpolation(IWorld world);

    public abstract void Dispose();

    /// <summary>
    /// Requests that the child implementations update to the world being
    /// provided.
    /// </summary>
    /// <param name="world">The world to update to.</param>
    protected abstract void UpdateToNewWorld(IWorld world);

    /// <summary>
    /// Performs the actual rendering of the automap.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    protected abstract void PerformAutomapRender(IWorld world, RenderInfo renderInfo);

    /// <summary>
    /// Performs the actual rendering commands.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    protected abstract void PerformRender(IWorld world, RenderInfo renderInfo);

    private bool IsWorldNotSeenBefore(IWorld world)
    {
        if (!m_lastRenderedWorld.TryGetTarget(out IWorld? lastWorld))
            return true;
        return !ReferenceEquals(lastWorld, world);
    }
}

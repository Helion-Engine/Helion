using System;
using Helion.Render.Legacy.Shared;
using Helion.World;

namespace Helion.Render.Legacy.Renderers;

/// <summary>
/// Responsible for rendering a world.
/// </summary>
public abstract class WorldRenderer : IDisposable
{
    private readonly WeakReference<WorldBase?> m_lastRenderedWorld = new WeakReference<WorldBase?>(null);

    /// <summary>
    /// Performs rendering on the world provided with the information for
    /// rendering.
    /// </summary>
    /// <param name="world">The world to render.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    public void Render(WorldBase world, RenderInfo renderInfo)
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

    public abstract void Dispose();

    /// <summary>
    /// Requests that the child implementations update to the world being
    /// provided.
    /// </summary>
    /// <param name="world">The world to update to.</param>
    protected abstract void UpdateToNewWorld(WorldBase world);

    /// <summary>
    /// Performs the actual rendering of the automap.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    protected abstract void PerformAutomapRender(WorldBase world, RenderInfo renderInfo);

    /// <summary>
    /// Performs the actual rendering commands.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="renderInfo">The rendering metadata.</param>
    protected abstract void PerformRender(WorldBase world, RenderInfo renderInfo);

    private bool IsWorldNotSeenBefore(WorldBase world)
    {
        if (!m_lastRenderedWorld.TryGetTarget(out WorldBase? lastWorld))
            return true;
        return !ReferenceEquals(lastWorld, world);
    }
}


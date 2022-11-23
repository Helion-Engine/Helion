using System;
using Helion.Render.Legacy.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.Legacy.Shared;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Sky;

/// <summary>
/// A sky that can be rendered. This might be a skybox, or a sphere to
/// emulate the vanilla sky, or a sky portal, etc.
/// </summary>
public interface ISkyComponent : IDisposable
{
    /// <summary>
    /// True if there is geometry to render with; otherwise if not then we
    /// shouldn't bother doing anything else since nothing can be drawn.
    /// </summary>
    bool HasGeometry { get; }

    /// <summary>
    /// Clears all the geometry in the world out so new geometry can be
    /// loaded.
    /// </summary>
    void Clear();

    /// <summary>
    /// Adds the triangle in the world where a sky should be drawn. This
    /// should be counter-clockwise.
    /// </summary>
    /// <param name="vertices">Sky vertices.</param>
    /// <param name="length">The number of vertices to copy.</param>
    void Add(SkyGeometryVertex[] vertices, int length);

    /// <summary>
    /// Renders the world geometry. It is assumed the stencil buffer can be
    /// written to and will be with this invocation.
    /// </summary>
    /// <param name="renderInfo">The position for rendering.</param>
    void RenderWorldGeometry(RenderInfo renderInfo);

    /// <summary>
    /// To be called after <see cref="RenderWorldGeometry"/>, such that it
    /// will draw the sky component wherever the stencil buffer was set to
    /// be written to.
    /// </summary>
    /// <param name="renderInfo">The position for rendering.</param>
    void RenderSky(RenderInfo renderInfo);
}

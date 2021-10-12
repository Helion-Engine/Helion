using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Window;

namespace Helion.Render;

/// <summary>
/// Manages and performs rendering.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// The name of the default surface which will always be guaranteed to
    /// exist. With certain hardware rendering implementations, this is the
    /// name for the default framebuffer.
    /// </summary>
    public const string DefaultSurfaceName = "Default";

    /// <summary>
    /// The default color that is drawn as the background. It's not exactly
    /// black so that we can see if there's a failure to draw.
    /// </summary>
    public static Color DefaultBackground => Color.FromArgb(16, 16, 16);

    /// <summary>
    /// The texture manager for the renderer.
    /// </summary>
    IRendererTextureManager Textures { get; }

    /// <summary>
    /// The window that this renderer belongs to.
    /// </summary>
    IWindow Window { get; }

    /// <summary>
    /// Gets the default renderable surface. This will be the one that is
    /// drawn to the screen as the final result. All drawing commands on
    /// different framebuffers that are intended to be displayed to the
    /// end user should be rendered to this surface at some point.
    /// </summary>
    IRenderableSurface DefaultSurface { get; }

    /// <summary>
    /// Gets a surface with a specific name, or creates a new one with
    /// a name if it does not exist.
    /// </summary>
    /// <remarks>
    /// If this is the <see cref="DefaultSurfaceName"/>, then the dimension
    /// may be ignored.
    /// </remarks>
    /// <param name="name">The name of the surface. This is used for
    /// looking up because surfaces are expensive.</param>
    /// <param name="dimension">The dimensions of the surface. This is
    /// for new texture framebuffers, and should have positive dimensions.
    /// </param>
    /// <returns>An existing surface with the same name, or a new one
    /// with the name.</returns>
    IRenderableSurface GetOrCreateSurface(string name, Dimension dimension);

    /// <summary>
    /// Performs any error checks that may throw if an error is found.
    /// </summary>
    /// <remarks>
    /// If the config has render debugging on, this should throw if an error
    /// exists.
    /// </remarks>
    void PerformThrowableErrorChecks();

    /// <summary>
    /// Force the pipeline to flush all the instructions.
    /// </summary>
    /// <remarks>
    /// Apparently if this is not done for renderers like OpenGL, it somehow
    /// clogs the pipeline with a lot of extra frames that are rendered, and
    /// some computers fall behind by 100ms or more.
    /// </remarks>
    void FlushPipeline();
}

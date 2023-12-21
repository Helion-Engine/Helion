using System;
using Helion.Geometry;

namespace Helion.Render.Common.Renderers;

/// <summary>
/// An object that acts as a place where commands can be issued to render
/// upon.
/// </summary>
public interface IRenderableSurface : IDisposable
{
    /// <summary>
    /// The name the default framebuffer uses.
    /// </summary>
    public static readonly string DefaultName = "Default";

    /// <summary>
    /// The width and height of the surface. This will likely be the
    /// size of the native window if it is the default surface, or it
    /// will be the dimension of the surface's texture.
    /// </summary>
    public Dimension Dimension { get; }

    /// <summary>
    /// Invokes rendering of all commands in the action, which should use
    /// the context provided.
    /// </summary>
    /// <param name="action">The rendering actions.</param>
    public void Render(Action<IRenderableSurfaceContext> action);
}

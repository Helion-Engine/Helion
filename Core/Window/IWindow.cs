using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render;

namespace Helion.Window;

/// <summary>
/// A window that reads input and renders.
/// </summary>
public interface IWindow : IDisposable
{
    /// <summary>
    /// The input manager for all of the input on this window.
    /// </summary>
    IInputManager InputManager { get; }

    /// <summary>
    /// The renderer that draws to this window.
    /// </summary>
    Renderer Renderer { get; }

    /// <summary>
    /// The current dimensions of this window.
    /// </summary>
    Dimension Dimension { get; }

    /// <summary>
    /// The current dimension of the framebuffer for the window.
    /// </summary>
    Dimension FramebufferDimension { get; }

    void SetMousePosition(Vec2I pos);
}

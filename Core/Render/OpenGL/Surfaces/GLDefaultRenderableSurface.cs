using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.Render.OpenGL.Framebuffers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Surfaces;

/// <summary>
/// A surface that is represented by the default framebuffer.
/// </summary>
public class GLDefaultRenderableSurface : GLRenderableSurface
{
    public override Dimension Dimension => Renderer.Window.Dimension;

    public GLDefaultRenderableSurface(GLRenderer renderer, GLHudRenderer hud, GLWorldRenderer world) :
        base(renderer, hud, world)
    {
    }

    protected override void Bind()
    {
        GLFramebuffer.PerformBind(0);
    }

    protected override void Unbind()
    {
        GLFramebuffer.PerformBind(0);
    }

    public override void Dispose()
    {
        // Does nothing, no components to dispose.
    }
}


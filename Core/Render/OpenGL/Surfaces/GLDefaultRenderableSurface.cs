using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using OpenTK.Graphics.ES11;

namespace Helion.Render.OpenGL.Surfaces
{
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

        public override void Render(Action<IRenderableSurfaceContext> action)
        {
            // The window dimension can change at any time, so this must always
            // be updated before handling various rendering commands.
            (int w, int h) = Dimension;
            GL.Viewport(0, 0, w, h);
            GL.Scissor(0, 0, w, h);
            
            // TODO: Re-use one instead of constantly reconstructing one.
            GLRenderableSurfaceContext context = new(this, HudRenderer, WorldRenderer);
            
            action(context);
        }
    }
}

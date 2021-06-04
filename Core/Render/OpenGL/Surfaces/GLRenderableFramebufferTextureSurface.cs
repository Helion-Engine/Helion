using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;

namespace Helion.Render.OpenGL.Surfaces
{
    public class GLRenderableFramebufferTextureSurface : GLRenderableSurface
    {
        public override Dimension Dimension { get; }

        public GLRenderableFramebufferTextureSurface(GLRenderer renderer, Dimension dimension, 
            GLHudRenderer hud, GLWorldRenderer world) 
            : base(renderer, hud, world)
        {
            Dimension = dimension;
        }

        public override void Render(Action<IRenderableSurfaceContext> action)
        {
            // TODO
        }
    }
}

using Helion.Geometry;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;

namespace Helion.Render.OpenGL.Surfaces
{
    public class GLRenderableFramebufferTextureSurface : GLRenderableSurface
    {
        // TODO: Framebuffer object goes here.
        
        public override Dimension Dimension { get; }

        public GLRenderableFramebufferTextureSurface(GLRenderer renderer, Dimension dimension, 
            GLHudRenderer hud, GLWorldRenderer world) 
            : base(renderer, hud, world)
        {
            Dimension = dimension;
        }

        protected override void Bind()
        {
            // TODO
        }

        protected override void Unbind()
        {
            // TODO
        }
    }
}

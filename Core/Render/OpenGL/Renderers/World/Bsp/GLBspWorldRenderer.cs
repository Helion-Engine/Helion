using Helion.Render.Common.Context;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Bsp
{
    /// <summary>
    /// A renderer that walks the BSP tree for deciding what to render and in
    /// what order.
    /// </summary>
    /// <remarks>
    /// This is not related to the GLBSP tool.
    /// </remarks>
    public class GLBspWorldRenderer : GLWorldRenderer
    {
        private bool m_disposed;
        
        ~GLBspWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override void Draw(IWorld world)
        {
            // TODO
        }
        
        internal override void Render(WorldRenderContext context)
        {
            // TODO
            
            base.Render(context);
        }

        protected override void PerformDispose()
        {
            if (m_disposed)
                return;
            
            // TODO

            m_disposed = true;
        }
    }
}

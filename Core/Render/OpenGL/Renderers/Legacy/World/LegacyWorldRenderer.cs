using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyWorldRenderer : WorldRenderer
    {
        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        
        public LegacyWorldRenderer(IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            m_textureManager = textureManager;
        }
        
        // TODO
        
        public override void Render(WorldBase world, RenderInfo renderInfo)
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            // TODO
        }
    }
}
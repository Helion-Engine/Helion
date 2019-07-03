using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public class WorldSkyRenderer
    {
        // For now we are only supporting one sky. We will have to change stuff
        // around if we want to support multiple skies later.
        private readonly WorldSkyComponent defaultSky = new WorldSkyComponent();

        public void Clear() => defaultSky.Clear();

        private void RenderGeometryToStencilBuffer(RenderInfo renderInfo)
        {
            //GL.ColorMask(false, false, false, false);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            
            defaultSky.RenderSkyGeometry(renderInfo);

            //GL.ColorMask(true, true, true, true);
        }

        private void RenderSkyboxAtStencilPixels(GLTexture skyTexture, RenderInfo renderInfo)
        {
            GL.StencilMask(0x00);
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.Disable(EnableCap.DepthTest);
            
            defaultSky.RenderSkybox(skyTexture, renderInfo);

            GL.Enable(EnableCap.DepthTest);
        }
        
        public void AddTriangle(WorldVertex first, WorldVertex second, WorldVertex third)
        {
            defaultSky.AddTriangle(first, second, third);
        }

        public void Render(GLTexture skyTexture, RenderInfo renderInfo)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.StencilMask(0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            RenderGeometryToStencilBuffer(renderInfo);
            //RenderSkyboxAtStencilPixels(skyTexture, renderInfo);
            
            GL.Disable(EnableCap.StencilTest);
        }
    }
}
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public class WorldSkyRenderer
    {
        private readonly DynamicArray<WorldVertex> skyVertices = new DynamicArray<WorldVertex>();
        private readonly int currentSkyIndex = 0x01;
        private readonly WorldSkyComponent defaultSky = new WorldSkyComponent();

        public void Clear() => skyVertices.Clear();

        private void SetStencilAttributes()
        {
            GL.ClearStencil(0x00);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            
            GL.StencilMask(0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        }

        private void RenderSkyboxAtStencilPixels()
        {
            GL.ColorMask(false, false, false, false);
            GL.StencilFunc(StencilFunction.Always, currentSkyIndex, 0xFF);
            
            // TODO: Draw geometry
            
            GL.ColorMask(true, true, true, true);
        }

        private void RenderWallsToStencilBuffer()
        {
            GL.StencilFunc(StencilFunction.Equal, currentSkyIndex, 0xFF);
            GL.Disable(EnableCap.DepthTest);
            
            // TODO: Bind and draw sky cube geometry
            
            GL.Enable(EnableCap.DepthTest);
        }

        public void Render()
        {
            GL.Enable(EnableCap.StencilTest);

            SetStencilAttributes();
            RenderWallsToStencilBuffer();
            RenderSkyboxAtStencilPixels();
            
            GL.Disable(EnableCap.StencilTest);
        }
    }
}
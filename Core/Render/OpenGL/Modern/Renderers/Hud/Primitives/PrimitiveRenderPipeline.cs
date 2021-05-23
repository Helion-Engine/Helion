using Helion.Render.OpenGL.Pipeline;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Primitives
{
    public class PrimitiveRenderPipeline : RenderPipeline<HudPrimitiveShader, HudPrimitiveVertex>
    {
        public PrimitiveRenderPipeline(string name, PrimitiveType drawType) : 
            base(name, BufferUsageHint.StreamDraw, drawType)
        {
        }
    }
}

using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticPlaneClipShaderMrt : StaticShader
{
    public StaticPlaneClipShaderMrt() : base("PlaneClipMRT")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticPlaneClipShaderMrt();
    }
}

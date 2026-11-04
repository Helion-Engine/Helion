using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticTransparentShader : StaticShader
{
    public StaticTransparentShader() : base("OIT Static Transparent")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticTransparentShader();
    }
}

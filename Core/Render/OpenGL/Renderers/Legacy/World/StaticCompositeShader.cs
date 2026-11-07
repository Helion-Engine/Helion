using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticCompositeShader : StaticShader
{
    public StaticCompositeShader() : base("OIT Static Composite")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticCompositeShader();
    }
}

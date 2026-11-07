using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationTransparentShader : InterpolationShader
{
    public InterpolationTransparentShader() : base("OIT Interpolation Transparent")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationTransparentShader();
    }
}

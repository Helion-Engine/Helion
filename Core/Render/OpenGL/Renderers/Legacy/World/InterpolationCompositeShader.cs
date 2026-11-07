using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

internal sealed class InterpolationCompositeShader : InterpolationShader
{
    public InterpolationCompositeShader() : base("OIT Composite")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationCompositeShader();
    }
}

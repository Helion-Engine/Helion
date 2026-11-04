using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationWallClipShader : InterpolationShader
{
    public InterpolationWallClipShader() : base("WallClip")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationWallClipShader();
    }
}

using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationWallClipAlphaShader : InterpolationShader
{
    public InterpolationWallClipAlphaShader() : base("WallClipAlhpa")
    {
    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationWallClipAlphaShader();
    }
}

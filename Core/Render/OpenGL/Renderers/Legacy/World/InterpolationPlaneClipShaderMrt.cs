using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationPlaneClipShaderMrt : InterpolationShader
{
    public InterpolationPlaneClipShaderMrt() : base("PlaneClip MRT")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationPlaneClipShaderMrt();
    }
}

using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

internal sealed class InterpolationPlaneClipAlphaShader : InterpolationShader
{
    public InterpolationPlaneClipAlphaShader() : base("PlaneAlphaClip")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new InterpolationPlaneClipAlphaShader();
    }
}

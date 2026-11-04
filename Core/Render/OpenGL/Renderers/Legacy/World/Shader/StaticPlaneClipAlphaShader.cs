using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

internal sealed class StaticPlaneClipAlphaShader : StaticShader
{
    public StaticPlaneClipAlphaShader() : base("PlaneClipAlpha")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticPlaneClipAlphaShader();
    }
}

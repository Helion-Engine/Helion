using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

internal sealed class StaticPlaneClipShader : StaticShader
{
    public StaticPlaneClipShader() : base("PlaneClip")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticPlaneClipShader();
    }
}

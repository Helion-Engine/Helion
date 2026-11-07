using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

internal sealed class StaticWallClipAlphaShader : StaticShader
{
    public StaticWallClipAlphaShader() : base("WallClipAlpha")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticWallClipAlphaShader();
    }
}

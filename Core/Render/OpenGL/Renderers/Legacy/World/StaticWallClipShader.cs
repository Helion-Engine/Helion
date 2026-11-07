using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticWallClipShader : StaticShader
{
    public StaticWallClipShader() : base("WallClip")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new StaticWallClipShader();
    }
}

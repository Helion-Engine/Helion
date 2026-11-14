using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillWallClipProgram : FloodFillProgram
{
    public FloodFillWallClipProgram() : base("WallClip")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new FloodFillWallClipProgram();
    }
}

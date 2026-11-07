using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

sealed class EntityTransparentProgram : EntityProgram
{
    public EntityTransparentProgram() : base("OIT Transparent")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new EntityTransparentProgram();
    }
}

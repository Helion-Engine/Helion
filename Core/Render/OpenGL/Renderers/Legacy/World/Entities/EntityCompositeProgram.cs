using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

internal sealed class EntityCompositeProgram : EntityProgram
{
    public EntityCompositeProgram() : base("OIT Composite")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new EntityCompositeProgram();
    }
}

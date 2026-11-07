using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityHealthBarProgram : EntityProgram
{
    public EntityHealthBarProgram() : base("HealthBar")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new EntityHealthBarProgram();
    }
}

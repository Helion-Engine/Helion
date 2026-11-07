using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityFuzzRefractionProgram : EntityProgram
{
    public EntityFuzzRefractionProgram() : base("OIT Fuzz Refraction")
    {

    }

    public override RenderProgram Recreate()
    {
        Dispose();
        return new EntityFuzzRefractionProgram();
    }
}

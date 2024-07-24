using Helion.World;

namespace Helion.Render.OpenGL.Commands.Types;

public struct DrawAutomapCommand(IWorld world)
{
    public IWorld World = world;
}

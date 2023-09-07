using Helion.RenderNew.Util;

namespace Helion.RenderNew.Interfaces.World;

public interface IRenderablePlayer : IRenderableEntity
{
    Camera GetCamera();
}
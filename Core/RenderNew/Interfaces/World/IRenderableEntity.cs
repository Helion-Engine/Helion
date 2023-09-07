using Helion.Geometry.Vectors;

namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableEntity
{
    Vec3F GetPosition();
    Vec3F GetVelocity();
}

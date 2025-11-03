using Helion.World.Entities;

namespace Helion.World;

internal struct NewTracerTargetData
{
    public Entity Entity;
    public Entity Owner;
    public Entity? TargetEntity;
    public double FieldOfViewRadians;
}

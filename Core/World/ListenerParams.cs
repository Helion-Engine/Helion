using Helion.Geometry.Vectors;
using Helion.World.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World;

public readonly struct ListenerParams
{
    public readonly Vec3D Position;
    public readonly double Angle;
    public readonly double Pitch;
    public readonly Entity Entity;

    public ListenerParams(Entity entity, double pitch)
    {
        Position = entity.Position;
        Angle = entity.AngleRadians;
        Pitch = pitch;
        Entity = entity;
    }
}

using Helion.World.Entities;
using Helion.World.Physics;

namespace Helion.World;

internal struct RadiusExplosionData
{
    public Entity DamageSource;
    public Entity AttackSource;
    public int Radius;
    public int MaxDamage;
    public Thrust Thrust;
}

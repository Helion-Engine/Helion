using Helion.Maps.Specials.ZDoom;

namespace Helion.World.Geometry.Lines;

public sealed class ObjectHealth
{
    public static readonly ObjectHealth Default = new();

    public int OriginalHealth;
    public int Health;
    public int HealthGroup;
    public ZDoomLineSpecialType Special;
    public bool DamageSpecial;
    public bool DeathSpecial;

    public bool Damage(int damage)
    {
        if (Health <= 0)
            return false;

        Health -= damage;
        return true;
    }
}

namespace Helion.Dehacked;

public class DehackedThing
{
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public int? ID { get; set; }
    public int? InitFrame { get; set; }
    public int? Hitpoints { get; set; }
    public int? FirstMovingFrame { get; set; }
    public int? AlertSound { get; set; }
    public int? ReactionTime { get; set; }
    public int? AttackSound { get; set; }
    public int? InjuryFrame { get; set; }
    public int? PainChance { get; set; }
    public int? PainSound { get; set; }
    public int? RipSound { get; set; }
    public int? CloseAttackFrame { get; set; }
    public int? FarAttackFrame { get; set; }
    public int? DeathFrame { get; set; }
    public int? ExplodingFrame { get; set; }
    public int? DeathSound { get; set; }
    public int? Speed { get; set; }
    public int? FastSpeed { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? MeleeRange { get; set; }
    public int? Mass { get; set; }
    public int? MisileDamage { get; set; }
    public int? ActionSound { get; set; }
    public uint? Bits { get; set; }
    public uint? Mbf21Bits { get; set; }
    public int? RespawnFrame { get; set; }
    public int? DroppedItem { get; set; }
    public int? InfightingGroup { get; set; }
    public int? ProjectileGroup { get; set; }
    public int? SplashGroup { get; set; }

    public override string ToString()
    {
        return $"[{Number}]{Name}";
    }
}

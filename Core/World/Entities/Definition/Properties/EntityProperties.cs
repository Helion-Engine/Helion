using System;
using Helion.Maps.Specials;
using Helion.World.Entities.Definition.Properties.Components;

namespace Helion.World.Entities.Definition.Properties;

public class EntityProperties
{
    public string ActiveSound = string.Empty;
    public string RipSound = String.Empty;
    public double Alpha = 1.0;
    public AmmoProperty Ammo = default;
    public SpecialArgs Args = default;
    public ArmorProperty Armor = default;
    public string AttackSound = string.Empty;
    public string BloodColor = string.Empty;
    public string BloodType = string.Empty;
    public DamageRangeProperty Damage;
    public double DamageFactor = 1.0;
    public double DeathHeight = 20.0 / 4;
    public string DeathSound = string.Empty;
    public int DefThreshold;
    public DropItemProperty? DropItem;
    public double FastSpeed;
    public int FloatSpeed = 4;
    public double Friction = 1.0;
    public int GibHealth = -1000;
    public double Gravity = 1.0;
    public int Health = 1000;
    public double Height = 16.0;
    public string HitObituary = string.Empty;
    public InventoryProperty Inventory = new();
    public double Mass = 100.0;
    public double MaxStepHeight = 24.0;
    public int MaxTargetRange;
    public int MeleeDamage;
    public double MeleeRange = 44;
    public string MeleeSound = string.Empty;
    public int MeleeThreshold;
    public int MinMissileChance = 200;
    public string Obituary = string.Empty;
    public int PainChance;
    public string PainSound = string.Empty;
    public PlayerProperty Player = new();
    public PowerupProperty Powerup = new();
    public int ProjectileKickBack = 100;
    public double ProjectilePassHeight;
    public double Radius = 20.0;
    public int ReactionTime = 8;
    public double Scale = 1.0;
    public string SeeSound = string.Empty;
    public int Threshold;
    public WeaponProperty Weapons = new();
    public HealthProperty? HealthProperty;
    public int? InfightingGroup;
    public int? ProjectileGroup;
    public int? SplashGroup;
    public int? RespawnTicks;
    public int RespawnDice = 4;
    public double SelfDamageFactor = 1;
    public EntityDefinition? TranslatedPickup;
    public EntityDefinition? TranslatedPickupDisplay;

    public double MonsterMovementSpeed;
    public double MissileMovementSpeed;

    public EntityProperties()
    {
    }
}

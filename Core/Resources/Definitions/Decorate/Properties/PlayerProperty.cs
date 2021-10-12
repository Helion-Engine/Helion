using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.Resources.Definitions.Decorate.Properties;

public struct PlayerProperty
{
    public double? AirCapacity;
    public int? AttackZOffset;
    public Range? ColorRange;
    public PlayerColorSetProperty? ColorSet;
    public PlayerColorSetFileProperty? ColorSetFile;
    public int? ClearColorSet;
    public string? CrouchSprite;
    public PlayerDamageScreenProperty? DamageScreenColor;
    public string? DisplayName;
    public string? Face;
    public PlayerFallingScreamSpeed? FallingScreamSpeed;
    public string? FlechetteType;
    public PlayerMoveProperty? ForwardMove;
    public double? GruntSpeed;
    public DecorateHealRadius? HealRadiusType;
    public HexenArmorProperty? HexenArmor;
    public string? InvulnerabilityMode;
    public double? JumpZ;
    public int? MaxHealth;
    public string? MorphWeapon;
    public int? MugShotMaxHealth;
    public string? Portrait;
    public int? RunHealth;
    public string? ScoreIcon;
    public PlayerMoveProperty? SideMove;
    public string? SoundClass;
    public string? SpawnClass;
    public List<PlayerStartItem>? StartItem;
    public int? TeleportFreezeTime;
    public double? UseRange;
    public List<PlayerWeaponSlot>? WeaponSlot;
    public double? ViewBob;
    public double? ViewHeight;
}

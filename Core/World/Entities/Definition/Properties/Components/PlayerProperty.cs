using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public class PlayerProperty
{
    public static readonly string SoundClassProperty = "player";

    public double AirCapacity = 1.0;
    public int AttackZOffset;
    public Range ColorRange = new(0, 0);
    public PlayerColorSetProperty? ColorSet;
    public PlayerColorSetFileProperty? ColorSetFile;
    public int ClearColorSet;
    public string CrouchSprite = string.Empty;
    public PlayerDamageScreenProperty? DamageScreenColor;
    public string DisplayName = string.Empty;
    public string Face = string.Empty;
    public PlayerFallingScreamSpeed FallingScreamSpeed = new(35, 40);
    public string FlechetteType = string.Empty;
    public PlayerMoveProperty ForwardMove = new(1, 1);
    public double GruntSpeed = 12.0;
    public DecorateHealRadius HealRadiusType = DecorateHealRadius.Health;
    public HexenArmorProperty? HexenArmor;
    public string InvulnerabilityMode = string.Empty;
    public double JumpZ = 8.0;
    public int MaxHealth = 100;
    public string MorphWeapon = string.Empty;
    public int MugShotMaxHealth;
    public string Portrait = string.Empty;
    public int RunHealth;
    public string ScoreIcon = string.Empty;
    public PlayerMoveProperty SideMove = new(1, 1);
    public string SoundClass = SoundClassProperty;
    public string SpawnClass = string.Empty;
    public List<PlayerStartItem> StartItem = new();
    public int TeleportFreezeTime;
    public double UseRange = 64;
    public List<PlayerWeaponSlot> WeaponSlot = new();
    public double ViewBob = 1;
    public double ViewHeight = 41;
}

using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public class PlayerProperty
{
    public double AirCapacity = 1.0;
    public int AttackZOffset;
    public Range ColorRange = new Range(0, 0);
    public PlayerColorSetProperty? ColorSet;
    public PlayerColorSetFileProperty? ColorSetFile;
    public int ClearColorSet;
    public string CrouchSprite = "";
    public PlayerDamageScreenProperty? DamageScreenColor;
    public string DisplayName = "";
    public string Face = "";
    public PlayerFallingScreamSpeed FallingScreamSpeed = new PlayerFallingScreamSpeed(35, 40);
    public string FlechetteType = "";
    public PlayerMoveProperty ForwardMove = new PlayerMoveProperty(1, 1);
    public double GruntSpeed = 12.0;
    public DecorateHealRadius HealRadiusType = DecorateHealRadius.Health;
    public HexenArmorProperty? HexenArmor;
    public string InvulnerabilityMode = "";
    public double JumpZ = 8.0;
    public int MaxHealth = 100;
    public string MorphWeapon = "";
    public int MugShotMaxHealth;
    public string Portrait = "";
    public int RunHealth;
    public string ScoreIcon = "";
    public PlayerMoveProperty SideMove = new PlayerMoveProperty(1, 1);
    public string SoundClass = "player";
    public string SpawnClass = "";
    public List<PlayerStartItem> StartItem = new List<PlayerStartItem>();
    public int TeleportFreezeTime;
    public double UseRange = 64;
    public List<PlayerWeaponSlot> WeaponSlot = new List<PlayerWeaponSlot>();
    public double ViewBob = 1;
    public double ViewHeight = 41;
}

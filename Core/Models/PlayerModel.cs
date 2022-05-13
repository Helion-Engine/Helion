using System;
using System.Collections.Generic;

namespace Helion.Models;

public class PlayerModel : EntityModel
{
    public readonly int Number;
    public readonly double PitchRadians;
    public readonly int DamageCount;
    public readonly int BonusCount;
    public readonly int ExtraLight;
    public readonly bool IsJumping;
    public readonly int JumpTics;
    public readonly int DeathTics;
    public readonly double ViewHeight;
    public readonly double ViewZ;
    public readonly double DeltaViewHeight;
    public readonly double Bob;
    public readonly int? Killer;
    public readonly int? Attacker;
    public readonly int KillCount;
    public readonly int ItemCount;
    public readonly int SecretsFound;
    public readonly string? Weapon;
    public readonly string? PendingWeapon;
    public readonly string? AnimationWeapon;
    public readonly double WeaponOffsetX;
    public readonly double WeaponOffsetY;
    public readonly int WeaponSlot;
    public readonly int WeaponSubSlot;
    public readonly InventoryModel Inventory;
    public readonly FrameStateModel? AnimationWeaponFrame;
    public readonly FrameStateModel? WeaponFlashFrame;
    public readonly IList<int> Cheats;

    public PlayerModel(int number, double pitchRadians, int damageCount, int bonusCount, int extraLight, bool isJumping, int jumpTics, int deathTics, double viewHeight, double viewZ, 
        double deltaViewHeight, double bob, int? killer, int? attacker, int killCount, int itemCount, int secretsFound, string? weapon, string? pendingWeapon, string? animationWeapon, 
        double weaponOffsetX, double weaponOffsetY, int weaponSlot, int weaponSubSlot, 
        InventoryModel inventory, FrameStateModel? animationWeaponFrame, FrameStateModel? weaponFlashFrame, IList<int> cheats)
    {
        Number = number;
        PitchRadians = pitchRadians;
        DamageCount = damageCount;
        BonusCount = bonusCount;
        ExtraLight = extraLight;
        IsJumping = isJumping;
        JumpTics = jumpTics;
        DeathTics = deathTics;
        ViewHeight = viewHeight;
        ViewZ = viewZ;
        DeltaViewHeight = deltaViewHeight;
        Bob = bob;
        Killer = killer;
        Attacker = attacker;
        KillCount = killCount;
        ItemCount = itemCount;
        SecretsFound = secretsFound;
        Weapon = weapon;
        PendingWeapon = pendingWeapon;
        AnimationWeapon = animationWeapon;
        WeaponOffsetX = weaponOffsetX;
        WeaponOffsetY = weaponOffsetY;
        WeaponSlot = weaponSlot;
        WeaponSubSlot = weaponSubSlot;
        Inventory = inventory;
        AnimationWeaponFrame = animationWeaponFrame;
        WeaponFlashFrame = weaponFlashFrame;
        Cheats = cheats;
    }
}

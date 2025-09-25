using Helion.World.Entities.Players;
using System.Collections.Generic;

namespace Helion.Models;

public class PlayerModel : EntityModel
{
    // JSON calls default constructor so the list allocations are in static Create methods
    public static PlayerModel Create()
    {
        return new()
        {
            Inventory = InventoryModel.Create(),
            Cheats = []
        };
    }

    public int Number;
    public double PitchRadians;
    public int DamageCount;
    public int BonusCount;
    public int ExtraLight;
    public bool IsJumping;
    public int JumpTics;
    public int DeathTics;
    public double ViewHeight;
    public double ViewZ;
    public double DeltaViewHeight;
    public double Bob;
    public double? WeaponBobX;
    public double? WeaponBobY;
    public int? Killer;
    public int? Attacker;
    public int KillCount;
    public int ItemCount;
    public int SecretsFound;
    public string? Weapon;
    public string? PendingWeapon;
    public string? AnimationWeapon;
    public double WeaponOffsetX;
    public double WeaponOffsetY;
    public int WeaponSlot;
    public int WeaponSubSlot;
    public InventoryModel Inventory = null!;
    public FrameStateModel? AnimationWeaponFrame;
    public FrameStateModel? WeaponFlashFrame;
    public List<int> Cheats = null!;
    public bool AttackDown;
    public PlayerStats PlayerStats;
}

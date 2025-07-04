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

    public int Number { get; set; }
    public double PitchRadians { get; set; }
    public int DamageCount { get; set; }
    public int BonusCount { get; set; }
    public int ExtraLight { get; set; }
    public bool IsJumping { get; set; }
    public int JumpTics { get; set; }
    public int DeathTics { get; set; }
    public double ViewHeight { get; set; }
    public double ViewZ { get; set; }
    public double DeltaViewHeight { get; set; }
    public double Bob { get; set; }
    public double? WeaponBobX { get; set; }
    public double? WeaponBobY { get; set; }
    public int? Killer { get; set; }
    public int? Attacker { get; set; }
    public int KillCount { get; set; }
    public int ItemCount { get; set; }
    public int SecretsFound { get; set; }
    public string? Weapon { get; set; }
    public string? PendingWeapon { get; set; }
    public string? AnimationWeapon { get; set; }
    public double WeaponOffsetX { get; set; }
    public double WeaponOffsetY { get; set; }
    public int WeaponSlot { get; set; }
    public int WeaponSubSlot { get; set; }
    public InventoryModel Inventory { get; set; } = null!;
    public FrameStateModel? AnimationWeaponFrame { get; set; }
    public FrameStateModel? WeaponFlashFrame { get; set; }
    public List<int> Cheats { get; set; } = null!;
    public bool AttackDown { get; set; }
    public PlayerStats PlayerStats { get; set; }
}

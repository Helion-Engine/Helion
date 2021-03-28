using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Models
{
    public class PlayerModel : EntityModel
    {
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
        public int? Killer { get; set; }
        public int? Attacker { get; set; }

        public string? Weapon { get; set; }
        public string? PendingWeapon { get; set; }
        public string? AnimationWeapon { get; set; }
        public Vec2D WeaponOffset { get; set; }
        public int WeaponSlot { get; set; }
        public int WeaponSubSlot { get; set; }
        public InventoryModel Inventory { get; set; }
        public FrameStateModel? AnimationWeaponFrame { get; set; }
        public FrameStateModel? WeaponFlashFrame { get; set; }
        public IList<int> Cheats { get; set; } = Array.Empty<int>();
    }
}

using Helion.Util.Geometry.Vectors;
using Newtonsoft.Json;

namespace Helion.Models
{
    public class PlayerModel : EntityModel
    {
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Killer;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Attacker;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Weapon;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? PendingWeapon;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? AnimationWeapon;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Vec2D WeaponOffset;
        public int WeaponSlot;
        public int WeaponSubSlot;
        public InventoryModel Inventory;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FrameStateModel? AnimationWeaponFrame;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FrameStateModel? WeaponFlashFrame;
    }
}

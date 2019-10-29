using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components
{
    public class WeaponProperty
    {
        public int AmmoGive;
        public int AmmoGive1;
        public int AmmoGive2;
        public string AmmoType = "";
        public string AmmoType1 = "";
        public string AmmoType2 = "";
        public int AmmoUse;
        public int AmmoUse1;
        public int AmmoUse2;
        public double BobRangeX;
        public double BobRangeY;
        public double BobSpeed;
        public WeaponBob BobStyle = WeaponBob.Normal;
        public bool DefaultKickBack;
        public int KickBack;
        public double LookScale;
        public int MinSelectionAmmo1;
        public int MinSelectionAmmo2;
        public string ReadySound = "";
        public int SelectionOrder;
        public string SisterWeapon = "";
        public int SlotNumber;
        public double SlotPriority;
        public string UpSound = "";
        public int YAdjust;
    }
}
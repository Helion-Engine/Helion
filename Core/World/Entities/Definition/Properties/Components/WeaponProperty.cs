using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public struct WeaponProperty
{ 
    public WeaponProperty()
    {
    }

    public int AmmoGive;
    public int AmmoGive1;
    public int AmmoGive2;
    public EntityDefinition? AmmoTypeDef;
    public string AmmoType = string.Empty;
    public string AmmoType1 = string.Empty;
    public string AmmoType2 = string.Empty;
    public bool AmmoUseSet = false;
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
    public string ReadySound = string.Empty;
    public int SelectionOrder = int.MaxValue;
    public string SisterWeapon = string.Empty;
    public int SlotNumber;
    public double SlotPriority;
    public string UpSound = string.Empty;
    public int YAdjust;
}

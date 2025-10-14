using System.ComponentModel;

namespace Helion.Util.Config.Components;

public enum WeaponSlots
{
    [Description("--")]
    None = -1,
    [Description("Chainsaw/Fist")]
    Melee = 1,
    [Description("Pistol")]
    Pistol = 2,
    [Description("SSG/Shotgun")]
    ShotgunOrSuperShotgun = 3,
    [Description("Chaingun")]
    Chaingun = 4,
    [Description("Rocket")]
    RocketLauncher = 5,
    [Description("Plasma")]
    PlasmaRifle = 6,
    [Description("BFG")]
    BFG9000 = 7
}
using Helion.Util.Config.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigPlayer: ConfigElement<ConfigPlayer>
{
    [ConfigInfo("Name of the player.")]
    [OptionMenu(OptionSectionType.Player, "Player Name", spacer: true)]
    public readonly ConfigValue<string> Name = new("Player", IfEmptyDefaultTo("Player"));

    [ConfigInfo("Gender of the player.")]
    [OptionMenu(OptionSectionType.Player, "Player Gender")]
    public readonly ConfigValue<PlayerGender> Gender = new(default, OnlyValidEnums<PlayerGender>());

    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "Group 1 1st", spacer: true)]
    public readonly ConfigValue<WeaponSlots> Group1Weapon1 = new(WeaponSlots.ShotgunOrSuperShotgun, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        2nd")]
    public readonly ConfigValue<WeaponSlots> Group1Weapon2 = new(WeaponSlots.ShotgunOrSuperShotgun, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        3rd")]
    public readonly ConfigValue<WeaponSlots> Group1Weapon3 = new(WeaponSlots.None, OnlyValidEnums<WeaponSlots>());

    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "Group 2 1st", spacer: true)]
    public readonly ConfigValue<WeaponSlots> Group2Weapon1 = new(WeaponSlots.RocketLauncher, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        2nd")]
    public readonly ConfigValue<WeaponSlots> Group2Weapon2 = new(WeaponSlots.Melee, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        3rd")]
    public readonly ConfigValue<WeaponSlots> Group2Weapon3 = new(WeaponSlots.Melee, OnlyValidEnums<WeaponSlots>());

    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "Group 3 1st", spacer: true)]
    public readonly ConfigValue<WeaponSlots> Group3Weapon1 = new(WeaponSlots.PlasmaRifle, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        2nd")]
    public readonly ConfigValue<WeaponSlots> Group3Weapon2 = new(WeaponSlots.BFG9000, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        3rd")]
    public readonly ConfigValue<WeaponSlots> Group3Weapon3 = new(WeaponSlots.None, OnlyValidEnums<WeaponSlots>());

    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "Group 4 1st", spacer: true)]
    public readonly ConfigValue<WeaponSlots> Group4Weapon1 = new(WeaponSlots.Chaingun, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        2nd")]
    public readonly ConfigValue<WeaponSlots> Group4Weapon2 = new(WeaponSlots.Pistol, OnlyValidEnums<WeaponSlots>());
    [ConfigInfo("")]
    [OptionMenu(OptionSectionType.Player, "        3rd")]
    public readonly ConfigValue<WeaponSlots> Group4Weapon3 = new(WeaponSlots.None, OnlyValidEnums<WeaponSlots>());
}

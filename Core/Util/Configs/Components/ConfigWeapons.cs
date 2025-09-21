using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Definition;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Helion.Util.Configs.Components;

public enum WeaponSwitch
{
    Always,
    Never,
    Preference,
    [Description("Preference Except Attacking")]
    PreferenceNoAttack
}

public class ConfigWeaponPreference : ConfigElement<ConfigWeaponPreference>
{
    const int PriorityMin = 0;
    const int PriorityMax = 10;
    private readonly Dictionary<string, ConfigValue<int>> m_weaponLookup;

    public ConfigWeaponPreference()
    {
        m_weaponLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Fist", Fist },
            { "Chainsaw", Chainsaw },
            { "Pistol", Pistol },
            { "Shotgun", Shotgun },
            { "SuperShotgun", SuperShotgun },
            { "Chaingun", Chaingun },
            { "RocketLauncher", RocketLauncher },
            { "PlasmaRifle", PlasmaRifle },
            { "BFG9000", BFG9000 }
        };
    }

    [ConfigInfo("Weapon pickup selection preference.")]
    [OptionMenu(OptionSectionType.Weapons, "Pickup Preference")]
    public ConfigValue<WeaponSwitch> Preference = new(WeaponSwitch.Always);

    [ConfigInfo("Disable selecting weapon with no ammo.")]
    [OptionMenu(OptionSectionType.Weapons, "No Ammo Select")]
    public ConfigValue<bool> NoAmmoSelect = new(true);

    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Weapons, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new("Weapon Priority");
    [ConfigInfo("Fist")]
    [OptionMenu(OptionSectionType.Weapons, "Fist", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1, spacer: true)]
    public ConfigValue<int> Fist = new(0, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("Chainsaw")]
    [OptionMenu(OptionSectionType.Weapons, "Chainsaw", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> Chainsaw = new(1, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("Pistol")]
    [OptionMenu(OptionSectionType.Weapons, "Pistol", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> Pistol = new(1, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("Shotgun")]
    [OptionMenu(OptionSectionType.Weapons, "Shotgun", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> Shotgun = new(2, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("SuperShotgun")]
    [OptionMenu(OptionSectionType.Weapons, "Super Shotgun", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> SuperShotgun = new(4, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("Chaingun")]
    [OptionMenu(OptionSectionType.Weapons, "Chaingun", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> Chaingun = new(3, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("RocketLauncher")]
    [OptionMenu(OptionSectionType.Weapons, "Rocket Launcher", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> RocketLauncher = new(1, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("PlasmaRifle")]
    [OptionMenu(OptionSectionType.Weapons, "Plasma Rifle", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> PlasmaRifle = new(5, ConfigFilters.Clamp(PriorityMin, PriorityMax));
    [ConfigInfo("BFG9000")]
    [OptionMenu(OptionSectionType.Weapons, "BFG9000", sliderMin: PriorityMin, sliderMax: PriorityMax, sliderStep: 1)]
    public ConfigValue<int> BFG9000 = new(2, ConfigFilters.Clamp(PriorityMin, PriorityMax));

    public int GetWeaponPriority(EntityDefinition definition)
    {
        if (m_weaponLookup.TryGetValue(definition.Name, out var value))
            return value.Value;

        return 0;
    }
}

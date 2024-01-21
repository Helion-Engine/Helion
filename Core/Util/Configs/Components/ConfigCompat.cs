using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigCompat
{
    [ConfigInfo("Vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Find shortest texture")]
    public readonly ConfigValue<bool> VanillaShortestTexture = new(true);

    [ConfigInfo("If dehacked should be preferred over decorate in the same archive.", demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use dehacked over decorate")]
    public readonly ConfigValue<bool> PreferDehacked = new(true);

    [ConfigInfo("Allow items to dropoff tall ledges.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Items drop off ledges")]
    public readonly ConfigValue<bool> AllowItemDropoff = new(true);

    [ConfigInfo("Use vanilla sector physics. Floors can move through ceiling. Only one move special per sector at a time.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla sector physics")]
    public readonly ConfigValue<bool> VanillaSectorPhysics = new(false);

    [ConfigInfo("Use vanilla movement physics. Velocity is maintained when hitting things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla movement physics")]
    public readonly ConfigValue<bool> VanillaMovementPhysics = new(false);

    [ConfigInfo("Emulate vanilla infinitely tall things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Infinitely tall things")]
    public readonly ConfigValue<bool> InfinitelyTallThings = new(false);

    [ConfigInfo("Things use their original vanilla heights for projectile collision checks.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla missile height collision")]
    public readonly ConfigValue<bool> MissileClip = new(false);

    [ConfigInfo("Limits lost souls spawn by pain elementals to 21.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Limit pain elemental lost souls to 21")]
    public readonly ConfigValue<bool> PainElementalLostSoulLimit = new(false);

    [ConfigInfo("Disables item drop tossing.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Disable item drop tossing")]
    public readonly ConfigValue<bool> NoTossDrops = new(false);

    [ConfigInfo("Use Doom's bugged stair building.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use bugged stair building")]
    public readonly ConfigValue<bool> Stairs = new(false);

    public void ResetToUserValues()
    {
        MissileClip.ResetToUserValue();
        VanillaShortestTexture.ResetToUserValue();
        VanillaSectorPhysics.ResetToUserValue();
        InfinitelyTallThings.ResetToUserValue();
        PainElementalLostSoulLimit.ResetToUserValue();
        NoTossDrops.ResetToUserValue();
        Stairs.ResetToUserValue();
        VanillaMovementPhysics.ResetToUserValue();
    }
}

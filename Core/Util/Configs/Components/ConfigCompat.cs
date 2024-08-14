using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigCompat
{
    [ConfigInfo("Use vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Find Shortest Texture")]
    public readonly ConfigValue<bool> VanillaShortestTexture = new(true);

    [ConfigInfo("Use DeHackEd over DECORATE if both are available.", demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use DeHackEd over DECORATE")]
    public readonly ConfigValue<bool> PreferDehacked = new(true);

    [ConfigInfo("Allow items to drop off tall ledges.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Items Drop Off Ledges")]
    public readonly ConfigValue<bool> AllowItemDropoff = new(true);

    [ConfigInfo("Use vanilla sector physics. Floors can move through ceilings. Only one move special per sector at a time.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Sector Physics")]
    public readonly ConfigValue<bool> VanillaSectorPhysics = new(false);

    [ConfigInfo("Use vanilla movement physics. Velocity is maintained when hitting things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Movement Physics")]
    public readonly ConfigValue<bool> VanillaMovementPhysics = new(false);

    [ConfigInfo("Use vanilla sector sound calculation. Sound is calculated from the center of the sector's bounding box.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Sector Sound")]
    public readonly ConfigValue<bool> VanillaSectorSound = new(false);

    [ConfigInfo("Emulate vanilla infinitely tall things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Infinitely Tall Things")]
    public readonly ConfigValue<bool> InfinitelyTallThings = new(false);

    [ConfigInfo("Things use their original vanilla heights for projectile collision checks.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Missile Height Collision")]
    public readonly ConfigValue<bool> MissileClip = new(false);

    [ConfigInfo("Limit lost souls spawned by pain elementals to 21.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Limit Pain Elemental Lost Souls to 21")]
    public readonly ConfigValue<bool> PainElementalLostSoulLimit = new(false);

    [ConfigInfo("Disable item drop tossing.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Disable Item Drop Tossing")]
    public readonly ConfigValue<bool> NoTossDrops = new(false);

    [ConfigInfo("Use Doom's bugged stair building.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use Bugged Stair Building")]
    public readonly ConfigValue<bool> Stairs = new(false);

    [ConfigInfo("Enable Doom 2 projectiles triggering walk specials.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Doom 2 Projectiles Trigger Walk Specials")]
    public readonly ConfigValue<bool> Doom2ProjectileWalkTriggers = new(false);

    [ConfigInfo("Use original Doom explosion behavior.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use Original Doom Explosion Behavior")]
    public readonly ConfigValue<bool> OriginalExplosion = new(false);

    [ConfigInfo("Enable vile ghosts.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vile Ghosts")]
    public readonly ConfigValue<bool> VileGhosts = new(false);

    [ConfigInfo("Enable Final Doom teleports. Disables forcing to floor.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Final Doom Teleport")]
    public readonly ConfigValue<bool> FinalDoomTeleport = new(false);

    [ConfigInfo("Enable MBF21 features.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Enable MBF21 Features")]
    public readonly ConfigValue<bool> Mbf21 = new(true);

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
        Doom2ProjectileWalkTriggers.ResetToUserValue();
        OriginalExplosion.ResetToUserValue();
        VileGhosts.ResetToUserValue();
        FinalDoomTeleport.ResetToUserValue();
        VanillaSectorSound.ResetToUserValue();
        Mbf21.ResetToUserValue();
    }
}

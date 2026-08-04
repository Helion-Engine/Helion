using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Resources.Definitions;
using Helion.Util.Configs.Impl;

namespace Helion.Util.Configs.Components;

public class ConfigCompat: ConfigElement<ConfigCompat>
{
    [ConfigInfo("Compatibility level for this session.  This setting is not saved to disk.", save: false)]
    [OptionMenu(OptionSectionType.Compatibility, "Compatibility Level")]
    public readonly ConfigValue<CompLevel> SessionCompatLevel = new(CompLevel.Undefined);

    [ConfigInfo("Use vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Find Shortest Texture", spacer: true)]
    public readonly ConfigCompatValue VanillaShortestTexture = new(CompatSetting.True);

    [ConfigInfo("Use DeHackEd over DECORATE if both are available.", demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use DeHackEd over DECORATE")]
    public readonly ConfigCompatValue PreferDehacked = new(CompatSetting.True);

    [ConfigInfo("Allow items to drop off tall ledges.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Items Drop Off Ledges")]
    public readonly ConfigCompatValue AllowItemDropoff = new(CompatSetting.True);

    [ConfigInfo("Use vanilla sector physics. Floors can move through ceilings. Only one move special per sector at a time.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Sector Physics")]
    public readonly ConfigCompatValue VanillaSectorPhysics = new(CompatSetting.False);

    [ConfigInfo("Use vanilla movement physics. Velocity is maintained when hitting things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Movement Physics")]
    public readonly ConfigCompatValue VanillaMovementPhysics = new(CompatSetting.True);

    [ConfigInfo("Use vanilla sector sound calculation. Sound is calculated from the center of the sector's bounding box.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Sector Sound")]
    public readonly ConfigCompatValue VanillaSectorSound = new(CompatSetting.False);

    [ConfigInfo("Emulate vanilla infinitely tall things.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Infinitely Tall Things")]
    public readonly ConfigCompatValue InfinitelyTallThings = new(CompatSetting.False);

    [ConfigInfo("Things use their original vanilla heights for projectile collision checks.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vanilla Missile Height Collision")]
    public readonly ConfigCompatValue MissileClip = new(CompatSetting.False);

    [ConfigInfo("Limit lost souls spawned by pain elementals to 21.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Limit Pain Elemental Lost Souls to 21")]
    public readonly ConfigCompatValue PainElementalLostSoulLimit = new(CompatSetting.False);

    [ConfigInfo("Disable item drop tossing.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Disable Item Drop Tossing")]
    public readonly ConfigCompatValue NoTossDrops = new(CompatSetting.False);

    [ConfigInfo("Use Doom's bugged stair building.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use Bugged Stair Building")]
    public readonly ConfigCompatValue Stairs = new(CompatSetting.False);

    [ConfigInfo("Enable Doom 2 projectiles triggering walk specials.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Doom 2 Projectiles Trigger Walk Specials")]
    public readonly ConfigCompatValue Doom2ProjectileWalkTriggers = new(CompatSetting.False);

    [ConfigInfo("Use original Doom explosion behavior.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Use Original Doom Explosion Behavior")]
    public readonly ConfigCompatValue OriginalExplosion = new(CompatSetting.False);

    [ConfigInfo("Enable vile ghosts.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Vile Ghosts")]
    public readonly ConfigCompatValue VileGhosts = new(CompatSetting.False);

    [ConfigInfo("Enable Final Doom teleports. Disables forcing to floor.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Final Doom Teleport")]
    public readonly ConfigCompatValue FinalDoomTeleport = new(CompatSetting.False);

    [ConfigInfo("Enable MBF21 features.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Enable MBF21 Features")]
    public readonly ConfigCompatValue Mbf21 = new(CompatSetting.True);

    [ConfigInfo("MBF Telefrag Behavior that disables monster telefrags on MAP30 and only allows for icon boss spawns.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Mbf Telefrag Behavior")]
    public readonly ConfigCompatValue MbfTelefrag = new(CompatSetting.True);

    [ConfigInfo("MBF Player movement that allowed player to move out of stuck lines.", serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "MBF Player Movement")]
    public readonly ConfigCompatValue MbfPlayerMovement = new(CompatSetting.True);

    public void ResetToUserValues()
    {
        AllowItemDropoff.ResetToUserValue();
        Doom2ProjectileWalkTriggers.ResetToUserValue();
        FinalDoomTeleport.ResetToUserValue();
        InfinitelyTallThings.ResetToUserValue();
        MissileClip.ResetToUserValue();
        NoTossDrops.ResetToUserValue();
        OriginalExplosion.ResetToUserValue();
        PainElementalLostSoulLimit.ResetToUserValue();
        Stairs.ResetToUserValue();
        VanillaMovementPhysics.ResetToUserValue();
        VanillaSectorPhysics.ResetToUserValue();
        VanillaSectorSound.ResetToUserValue();
        VanillaShortestTexture.ResetToUserValue();
        VileGhosts.ResetToUserValue();
        Mbf21.ResetToUserValue();
        MbfTelefrag.ResetToUserValue();
    }
}

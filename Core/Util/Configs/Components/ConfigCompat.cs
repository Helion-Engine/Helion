using Helion.Resources.Definitions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public static class ConfigCompatLabels
{
    public const string DefaultCompatLevelLabel = "Default Compatibility Level";
    public const string SessionCompatLevelLabel = "Session Compatibility Level";
}

public class ConfigCompat
{
    [ConfigInfo("Default compatibility level. This will be used when loaded WADs do not set the COMPLVL flag.")]
    [OptionMenu(OptionSectionType.Compatibility, ConfigCompatLabels.DefaultCompatLevelLabel)]
    public readonly ConfigValue<CompLevel> DefaultCompatLevel = new(CompLevel.Undefined);

    [ConfigInfo("Compatibility level for this session. This setting is not saved to disk.", save: false)]
    [OptionMenu(OptionSectionType.Compatibility, ConfigCompatLabels.SessionCompatLevelLabel)]
    public readonly ConfigValue<CompLevel> SessionCompatLevel = new(CompLevel.Undefined);

    [ConfigInfo("Use vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true, demo: true)]
    [OptionMenu(OptionSectionType.Compatibility, "Find Shortest Texture", spacer: true)]
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

    private bool m_changingCompatLevel = false;

    public void SetChangingCompatLevel(bool newValue)
    {
        m_changingCompatLevel = newValue;
    }

    private void SetCompatLevelToUndefined(object? sender, bool e)
    {
        if (m_changingCompatLevel ||
            ((CompLevel)this.DefaultCompatLevel.ObjectValue == CompLevel.Undefined &&
            (CompLevel)this.SessionCompatLevel.ObjectValue == CompLevel.Undefined))
        {
            // Avoid recursion
            return;
        }

        m_changingCompatLevel = true;
        this.DefaultCompatLevel.Set(CompLevel.Undefined);
        this.SessionCompatLevel.Set(CompLevel.Undefined, writeToConfig: false);
        m_changingCompatLevel = false;
    }

    public void ActivateChangeHandlers()
    {
        // If the user sets values for any of these items directly, we want to set the Compat levels to Undefined.
        AllowItemDropoff.OnChanged += this.SetCompatLevelToUndefined;
        Doom2ProjectileWalkTriggers.OnChanged += this.SetCompatLevelToUndefined;
        FinalDoomTeleport.OnChanged += this.SetCompatLevelToUndefined;
        InfinitelyTallThings.OnChanged += this.SetCompatLevelToUndefined;
        MissileClip.OnChanged += this.SetCompatLevelToUndefined;
        NoTossDrops.OnChanged += this.SetCompatLevelToUndefined;
        OriginalExplosion.OnChanged += this.SetCompatLevelToUndefined;
        PainElementalLostSoulLimit.OnChanged += this.SetCompatLevelToUndefined;
        Stairs.OnChanged += this.SetCompatLevelToUndefined;
        VanillaMovementPhysics.OnChanged += this.SetCompatLevelToUndefined;
        VanillaSectorPhysics.OnChanged += this.SetCompatLevelToUndefined;
        VanillaSectorSound.OnChanged += this.SetCompatLevelToUndefined;
        VanillaShortestTexture.OnChanged += this.SetCompatLevelToUndefined;
        VileGhosts.OnChanged += this.SetCompatLevelToUndefined;
        Mbf21.OnChanged += this.SetCompatLevelToUndefined;
    }

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
    }

    public void ResetToDefaultValues()
    {
        m_changingCompatLevel = true;

        AllowItemDropoff.ResetToDefaultValue();
        Doom2ProjectileWalkTriggers.ResetToDefaultValue();
        FinalDoomTeleport.ResetToDefaultValue();
        InfinitelyTallThings.ResetToDefaultValue();
        MissileClip.ResetToDefaultValue();
        NoTossDrops.ResetToDefaultValue();
        OriginalExplosion.ResetToDefaultValue();
        PainElementalLostSoulLimit.ResetToDefaultValue();
        Stairs.ResetToDefaultValue();
        VanillaMovementPhysics.ResetToDefaultValue();
        VanillaSectorPhysics.ResetToDefaultValue();
        VanillaSectorSound.ResetToDefaultValue();
        VanillaShortestTexture.ResetToDefaultValue();
        VileGhosts.ResetToDefaultValue();
        Mbf21.ResetToDefaultValue();

        m_changingCompatLevel = false;
    }
}

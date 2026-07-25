using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Loggers;
using System;

namespace Helion.Resources.Definitions;

public enum CompLevel
{
    Undefined,
    Vanilla,
    Boom,
    Mbf,
    Mbf21
}

public class CompLevelDefinition
{
    public CompLevel CompLevel;
    private bool m_setting;

    public CompLevelDefinition()
    {
        CompLevel = CompLevel.Undefined;
    }

    public void Parse(string text)
    {
        if (!Enum.TryParse(text.Trim(), ignoreCase: true, out CompLevel))
        {
            CompLevel = CompLevel.Undefined;
        }
    }

    public void Apply(IConfig config, bool reset = false)
    {
        // Avoid possible recursion if invoked via event handler
        if (m_setting)
            return;

        m_setting = true;
        ApplyInternal(config, reset);
        m_setting = false;
    }

    private void ApplyInternal(IConfig config, bool reset)
    {
        if ((CompLevel)config.Compatibility.SessionCompatLevel.ObjectValue != CompLevel)
            config.Compatibility.SessionCompatLevel.Set(CompLevel, writeToConfig: false);

        if (reset)
            config.Compatibility.ResetToUserValues();

        if (CompLevel == CompLevel.Undefined)
            return;

        var compat = config.Compatibility;
        switch (CompLevel)
        {
            case CompLevel.Vanilla:
                compat.AllowItemDropoff.SetIfMutable(false);
                compat.Doom2ProjectileWalkTriggers.SetIfMutable(true);
                compat.InfinitelyTallThings.SetIfMutable(true);
                compat.MissileClip.SetIfMutable(true);
                compat.OriginalExplosion.SetIfMutable(true);
                compat.PainElementalLostSoulLimit.SetIfMutable(true);
                compat.Stairs.SetIfMutable(true);
                compat.VanillaMovementPhysics.SetIfMutable(true);
                compat.VanillaSectorPhysics.SetIfMutable(true);
                compat.VanillaSectorSound.SetIfMutable(true);
                compat.VanillaShortestTexture.SetIfMutable(true);
                compat.VileGhosts.SetIfMutable(true);
                compat.MbfTelefrag.SetIfMutable(false);

                compat.Mbf21.SetIfMutable(false);
                break;
            case CompLevel.Boom:
                SetBoomCompat(compat, mbf21: false, mbfTelefrag: false);
                break;
            case CompLevel.Mbf:
                SetBoomCompat(compat, mbf21: false, mbfTelefrag: true);
                break;
            case CompLevel.Mbf21:
                SetBoomCompat(compat, mbf21: true, mbfTelefrag: true);
                break;
        }

        HelionLog.Info($"Comp level set to {CompLevel}");
    }

    private static void SetBoomCompat(ConfigCompat compat, bool mbf21, bool mbfTelefrag)
    {
        compat.AllowItemDropoff.SetIfMutable(true);
        compat.Stairs.SetIfMutable(false);
        compat.VanillaSectorPhysics.SetIfMutable(false);
        compat.VanillaShortestTexture.SetIfMutable(false);

        compat.MbfTelefrag.SetIfMutable(mbfTelefrag);
        compat.Mbf21.SetIfMutable(mbf21);
    }
}

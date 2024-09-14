using Helion.Util.Configs;
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
        if ((CompLevel)config.Compatibility.CompatLevel.ObjectValue != CompLevel)
            config.Compatibility.CompatLevel.Set(CompLevel, writeToConfig: false);

        if (reset)
            config.Compatibility.ResetToUserValues();

        if (CompLevel == CompLevel.Undefined)
        {
            return;
        }

        var compat = config.Compatibility;
        switch (CompLevel)
        {
            case CompLevel.Vanilla:
                compat.AllowItemDropoff.Set(false, writeToConfig: false);
                compat.Doom2ProjectileWalkTriggers.Set(true, writeToConfig: false);
                compat.InfinitelyTallThings.Set(true, writeToConfig: false);
                compat.MissileClip.Set(true, writeToConfig: false);
                compat.OriginalExplosion.Set(true, writeToConfig: false);
                compat.PainElementalLostSoulLimit.Set(true, writeToConfig: false);
                compat.Stairs.Set(true, writeToConfig: false);
                compat.VanillaMovementPhysics.Set(true, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(true, writeToConfig: false);
                compat.VanillaSectorSound.Set(true, writeToConfig: false);
                compat.VanillaShortestTexture.Set(true, writeToConfig: false);

                compat.Mbf21.Set(false, writeToConfig: false);
                break;
            case CompLevel.Boom:
            case CompLevel.Mbf:
                compat.AllowItemDropoff.Set(true, writeToConfig: false);
                compat.Stairs.Set(false, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: false);
                compat.VanillaShortestTexture.Set(false, writeToConfig: false);

                compat.Mbf21.Set(false, writeToConfig: false);
                break;
            case CompLevel.Mbf21:
                compat.AllowItemDropoff.Set(true, writeToConfig: false);
                compat.Stairs.Set(false, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: false);
                compat.VanillaShortestTexture.Set(false, writeToConfig: false);

                compat.Mbf21.Set(true, writeToConfig: false);
                break;
        }

        HelionLog.Info($"Comp level set to {CompLevel}");
    }
}

using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using NLog;

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
        text = text.Trim();
        CompLevel = CompLevel.Undefined;
        if (text.EqualsIgnoreCase("vanilla"))
            CompLevel = CompLevel.Vanilla;
        else  if (text.EqualsIgnoreCase("boom"))
            CompLevel = CompLevel.Boom;
        else if (text.EqualsIgnoreCase("mbf"))
            CompLevel = CompLevel.Mbf;
        else if (text.EqualsIgnoreCase("mbf21"))
            CompLevel = CompLevel.Mbf21;
    }

    public void Apply(IConfig config)
    {
        if (CompLevel == CompLevel.Undefined)
            return;

        var compat = config.Compatibility;
        switch (CompLevel)
        {
            case CompLevel.Vanilla:
                compat.InfinitelyTallThings.Set(true, writeToConfig: false);
                compat.OriginalExplosion.Set(true, writeToConfig: false);
                compat.VanillaMovementPhysics.Set(true, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(true, writeToConfig: false);
                compat.VanillaSectorSound.Set(true, writeToConfig: false);
                compat.MissileClip.Set(true, writeToConfig: false);
                compat.Stairs.Set(true, writeToConfig: false);
                compat.PainElementalLostSoulLimit.Set(true, writeToConfig: false);
                compat.Doom2ProjectileWalkTriggers.Set(true, writeToConfig: false);
                compat.AllowItemDropoff.Set(false, writeToConfig: false);
                compat.Mbf21.Set(false, writeToConfig: false);
                break;
            case CompLevel.Boom:
            case CompLevel.Mbf:
                compat.AllowItemDropoff.Set(true, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: false);
                compat.Stairs.Set(false, writeToConfig: false);
                compat.Mbf21.Set(false, writeToConfig: false);
                break;
            case CompLevel.Mbf21:
                compat.AllowItemDropoff.Set(true, writeToConfig: false);
                compat.Mbf21.Set(true, writeToConfig: false);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: false);
                compat.Stairs.Set(false, writeToConfig: false);
                break;
        }

        HelionLog.Info($"Comp level set to {CompLevel}");
    }
}

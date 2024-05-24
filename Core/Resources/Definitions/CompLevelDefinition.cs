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
                compat.InfinitelyTallThings.SetWithNoWriteConfig(true);
                compat.OriginalExplosion.SetWithNoWriteConfig(true);
                compat.VanillaMovementPhysics.SetWithNoWriteConfig(true);
                compat.VanillaSectorPhysics.SetWithNoWriteConfig(true);
                compat.VanillaSectorSound.SetWithNoWriteConfig(true);
                compat.MissileClip.SetWithNoWriteConfig(true);
                compat.Stairs.SetWithNoWriteConfig(true);
                compat.PainElementalLostSoulLimit.SetWithNoWriteConfig(true);
                compat.Doom2ProjectileWalkTriggers.SetWithNoWriteConfig(true);
                compat.AllowItemDropoff.SetWithNoWriteConfig(false);
                compat.Mbf21.SetWithNoWriteConfig(false);
                break;
            case CompLevel.Boom:
            case CompLevel.Mbf:
                compat.AllowItemDropoff.SetWithNoWriteConfig(true);
                compat.VanillaSectorPhysics.SetWithNoWriteConfig(false);
                compat.Stairs.SetWithNoWriteConfig(false);
                compat.Mbf21.SetWithNoWriteConfig(false);
                break;
            case CompLevel.Mbf21:
                compat.AllowItemDropoff.SetWithNoWriteConfig(true);
                compat.Mbf21.SetWithNoWriteConfig(true);
                compat.VanillaSectorPhysics.SetWithNoWriteConfig(false);
                compat.Stairs.SetWithNoWriteConfig(false);
                break;
        }

        HelionLog.Info($"Comp level set to {CompLevel}");
    }
}

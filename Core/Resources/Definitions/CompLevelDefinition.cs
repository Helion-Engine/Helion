using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;
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

    public void Apply(IConfig config, ILogger log)
    {
        if (CompLevel != CompLevel.Vanilla)
            return;

        log.Info($"Comp level set to {CompLevel}");

        var compat = config.Compatibility;
        compat.VanillaMovementPhysics.SetWithNoWriteConfig(true);
        compat.VanillaSectorPhysics.SetWithNoWriteConfig(true);
        compat.MissileClip.SetWithNoWriteConfig(true);
        compat.Stairs.SetWithNoWriteConfig(true);
        compat.PainElementalLostSoulLimit.SetWithNoWriteConfig(true);
        compat.AllowItemDropoff.SetWithNoWriteConfig(false);
    }
}

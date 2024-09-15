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

    public void Apply(IConfig config, bool reset = false, bool save = false)
    {
        // Avoid possible recursion if invoked via event handler
        if ((CompLevel)config.Compatibility.SessionCompatLevel.ObjectValue != CompLevel)
            config.Compatibility.SessionCompatLevel.Set(CompLevel, writeToConfig: false);

        if (reset)
            config.Compatibility.ResetToUserValues();

        if (CompLevel == CompLevel.Undefined)
        {
            return;
        }

        var compat = config.Compatibility;
        compat.SetChangingCompatLevel(true);

        switch (CompLevel)
        {
            case CompLevel.Vanilla:
                compat.AllowItemDropoff.Set(false, writeToConfig: save);
                compat.Doom2ProjectileWalkTriggers.Set(true, writeToConfig: save);
                compat.InfinitelyTallThings.Set(true, writeToConfig: save);
                compat.MissileClip.Set(true, writeToConfig: save);
                compat.OriginalExplosion.Set(true, writeToConfig: save);
                compat.PainElementalLostSoulLimit.Set(true, writeToConfig: save);
                compat.Stairs.Set(true, writeToConfig: save);
                compat.VanillaMovementPhysics.Set(true, writeToConfig: save);
                compat.VanillaSectorPhysics.Set(true, writeToConfig: save);
                compat.VanillaSectorSound.Set(true, writeToConfig: save);
                compat.VanillaShortestTexture.Set(true, writeToConfig: save);

                compat.Mbf21.Set(false, writeToConfig: save);
                break;
            case CompLevel.Boom:
            case CompLevel.Mbf:
                compat.AllowItemDropoff.Set(true, writeToConfig: save);
                compat.Stairs.Set(false, writeToConfig: save);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: save);
                compat.VanillaShortestTexture.Set(false, writeToConfig: save);

                compat.Mbf21.Set(false, writeToConfig: save);
                break;
            case CompLevel.Mbf21:
                compat.AllowItemDropoff.Set(true, writeToConfig: save);
                compat.Stairs.Set(false, writeToConfig: save);
                compat.VanillaSectorPhysics.Set(false, writeToConfig: save);
                compat.VanillaShortestTexture.Set(false, writeToConfig: save);

                compat.Mbf21.Set(true, writeToConfig: save);
                break;
        }

        compat.SetChangingCompatLevel(false);

        HelionLog.Info($"Comp level set to {CompLevel}");
    }
}

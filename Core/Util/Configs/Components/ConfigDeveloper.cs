using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigDeveloperRender: ConfigElement<ConfigDeveloperRender>
{
    [ConfigInfo("Draw rendering debug information.", save: false)]
    public readonly ConfigValue<bool> Debug = new(false);

    [ConfigInfo("Draw the tracers from autoaim and shooting for the player.", save: false)]
    public readonly ConfigValue<bool> Tracers = new(false);
}

public class ConfigDeveloper: ConfigElement<ConfigDeveloper>
{
    public readonly ConfigDeveloperRender Render = new();

    [ConfigInfo("Mark flooded areas on the automap.", save: true)]
    public readonly ConfigValue<bool> MarkFlood = new(false);

    [ConfigInfo("Log marked special info.", save: true)]
    public readonly ConfigValue<bool> LogMarkSpecials = new(false);

    [ConfigInfo("Use ReversedZ.", save: true, restartRequired: true)]
    public readonly ConfigValue<bool> UseReversedZ = new(false);

    [ConfigInfo("Force usage of ReversedZ. Only used if Developer.ReversedZ is set.", save: true, restartRequired: true)]
    public readonly ConfigValue<bool> ReversedZ = new(false);

    [ConfigInfo("Log garbage collection events.", save: true)]
    public readonly ConfigValue<bool> LogGC = new(false);

    [ConfigInfo("Logs profiled execution times that take longer than the provided millisecond value. 0=off", save: true)]
    public readonly ConfigValue<double> ProfilerTimeTrigger = new(0);

    [ConfigInfo("Force texel fetches for brightmaps, sector color, and sector fog", save: true)]
    public readonly ConfigValue<bool> ForceTexelFetch = new(false);
}

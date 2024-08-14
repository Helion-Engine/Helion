using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigDeveloperRender
{
    [ConfigInfo("Draw rendering debug information.", save: false)]
    public readonly ConfigValue<bool> Debug = new(false);

    [ConfigInfo("Draw the tracers from autoaim and shooting for the player.", save: false)]
    public readonly ConfigValue<bool> Tracers = new(false);
}

public class ConfigDeveloper
{
    public readonly ConfigDeveloperRender Render = new();

    [ConfigInfo("Mark flooded areas on the automap.", save: true)]
    public readonly ConfigValue<bool> MarkFlood = new(false);

    [ConfigInfo("Log marked special info.", save: true)]
    public readonly ConfigValue<bool> LogMarkSpecials = new(false);

    [ConfigInfo("Flood opposing testing.", save: true)]
    public readonly ConfigValue<bool> FloodOpposing = new(false);

    [ConfigInfo("Use ReversedZ.", save: true, restartRequired: true)]
    public readonly ConfigValue<bool> UseReversedZ = new(false);

    [ConfigInfo("Force usage of ReversedZ. Only used if Developer.ReversedZ is set.", save: true, restartRequired: true)]
    public readonly ConfigValue<bool> ReversedZ = new(false);
}

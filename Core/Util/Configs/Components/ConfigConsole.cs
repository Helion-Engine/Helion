using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigConsole: ConfigElement<ConfigConsole>
{
    [ConfigInfo("Number of messages the console buffer holds before discarding old ones.")]
    [OptionMenu(OptionSectionType.Console, "Max Messages")]
    public readonly ConfigValue<int> MaxMessages = new(256, Greater(0));

    [ConfigInfo("Font size.")]
    [OptionMenu(OptionSectionType.Console, "Font Size")]
    public readonly ConfigValue<int> FontSize = new(32, Greater(15));

    [ConfigInfo("Transparency.")]
    [OptionMenu(OptionSectionType.Console, "Transparency")]
    public readonly ConfigValue<double> Transparency = new(0.6, ClampNormalized);
}

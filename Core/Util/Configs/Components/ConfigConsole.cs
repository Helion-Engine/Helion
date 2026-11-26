using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigConsole: ConfigElement<ConfigConsole>
{
    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Misc, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new("Console");

    [ConfigInfo("Number of messages the console buffer holds before discarding old ones.")]
    [OptionMenu(OptionSectionType.Misc, "Max Messages", sliderMin: 0, sliderMax: 2000, sliderStep: 10)]
    public readonly ConfigValue<int> MaxMessages = new(256, Greater(0));

    [ConfigInfo("Font size.")]
    [OptionMenu(OptionSectionType.Misc, "Font Size", sliderMin: 15, sliderMax: 64, sliderStep: 1)]
    public readonly ConfigValue<int> FontSize = new(32, Greater(15));

    [ConfigInfo("Transparency.")]
    [OptionMenu(OptionSectionType.Misc, "Transparency", sliderMin: 0, sliderMax: 1, sliderStep: .05)]
    public readonly ConfigValue<double> Transparency = new(0.6, ClampNormalized);
}

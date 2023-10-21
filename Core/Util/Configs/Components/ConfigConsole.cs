using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigConsole
{
    [ConfigInfo("The number of messages the console buffer holds before discarding old ones.")]
    [OptionMenu(OptionSectionType.Console, "Max messages")]
    public readonly ConfigValue<int> MaxMessages = new(256, Greater(0));

    [ConfigInfo("Font size.")]
    [OptionMenu(OptionSectionType.Console, "Font size")]
    public readonly ConfigValue<int> FontSize = new(32, Greater(15));
}

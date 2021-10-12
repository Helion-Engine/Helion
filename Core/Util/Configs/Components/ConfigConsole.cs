using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigConsole
{
    [ConfigInfo("The number of messages the console buffer holds before discarding old ones.")]
    public readonly ConfigValue<int> MaxMessages = new(256, Greater(0));
}


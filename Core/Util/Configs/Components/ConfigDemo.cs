using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigDemo: ConfigElement<ConfigDemo>
{
    [ConfigInfo("Playback tick multiplier rounded to the nearest tick. (0.5 = half speed, 2 = double speed)", save: true)]
    [OptionMenu(OptionSectionType.Demo, "Playback Speed")]
    public readonly ConfigValue<double> PlaybackSpeed = new(1);
}

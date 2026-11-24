using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigSlowTick: ConfigElement<ConfigSlowTick>
{
    private const int SlowTickMultiplierMax = 10;

    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Misc, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new("Slowtick");

    [ConfigInfo("Enables slow ticking; reduces overhead by updating distant actors less frequently. ", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Enable")]
    public readonly ConfigValue<bool> Enabled = new(false);

    [ConfigInfo("Distance threshold for A_Look and A_Chase. Actors beyond this distance threshold are updated less frequently. 0 = Disabled.", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Distance Threshold", sliderMin: 0, sliderMax: int.MaxValue, sliderStep: 1000)]
    public readonly ConfigValue<int> Distance = new(2000, Clamp(0, int.MaxValue));

    [ConfigInfo("Number of times to skip setting a new chase direction when an actor fails to move due to an obstruction. 0 = Disabled.", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Chase Failure Skip Count", spacer: true, sliderMin: 0, sliderMax: SlowTickMultiplierMax, sliderStep: 1)]
    public readonly ConfigValue<int> ChaseFailureSkipCount = new(4, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("Tick multiplier for chase beyond distance threshold. 0 = Disabled.", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Chase Multiplier", sliderMin: 0, sliderMax: SlowTickMultiplierMax, sliderStep: 1)]
    public readonly ConfigValue<int> ChaseMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("Tick multiplier for look beyond distance threshold. 0 = Disabled.", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Look Multiplier", sliderMin: 0, sliderMax: SlowTickMultiplierMax, sliderStep: 1)]
    public readonly ConfigValue<int> LookMultiplier = new(2, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("Tick multiplier for tracers beyond distance threshold. 0 = Disabled.", demo: true)]
    [OptionMenu(OptionSectionType.Misc, "Tracer Multiplier", sliderMin: 0, sliderMax: SlowTickMultiplierMax, sliderStep: 1)]
    public readonly ConfigValue<int> TracerMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));
}

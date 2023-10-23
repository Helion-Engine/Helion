using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigSlowTick
{
    private const int SlowTickMultiplierMax = 10;

    [ConfigInfo("Enables slow ticking properties.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Enable")]
    public readonly ConfigValue<bool> Enabled = new(false);

    [ConfigInfo("Distance to start slow ticking things for A_Look and A_Chase. 0 = disabled.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Distance")]
    public readonly ConfigValue<int> Distance = new(2000, Clamp(0, int.MaxValue));

    [ConfigInfo("Number of times to skip setting a new chase direction on movement failures. 0 = disabled.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Chase failure skip count", spacer: true)]
    public readonly ConfigValue<int> ChaseFailureSkipCount = new(4, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("How much to multiply the ticks for chase with SlowTickDistance. 0 = disabled.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Chase multiplier")]
    public readonly ConfigValue<int> ChaseMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("How much to multiply the ticks for look with SlowTickDistance. 0 = disabled.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Look multiplier")]
    public readonly ConfigValue<int> LookMultiplier = new(2, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("How much to multiply the ticks for tracers with SlowTickDistance. 0 = disabled.", demo: true)]
    [OptionMenu(OptionSectionType.SlowTick, "Tracer multiplier")]
    public readonly ConfigValue<int> TracerMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));
}

using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigSlowTick
{
    const int SlowTickMultiplierMax = 10;

    [ConfigInfo("Distance to start slow ticking things for A_Look and A_Chase.", demo: true)]
    public readonly ConfigValue<int> Distance = new(0, Clamp(0, int.MaxValue));

    [ConfigInfo("How much to multiply the ticks for chase with SlowTickDistance.", demo: true)]
    public readonly ConfigValue<int> ChaseMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("How much to multiply the ticks for look with SlowTickDistance.", demo: true)]
    public readonly ConfigValue<int> LookMultiplier = new(2, Clamp(0, SlowTickMultiplierMax));

    [ConfigInfo("How much to multiply the ticks for tracers with SlowTickDistance.", demo: true)]
    public readonly ConfigValue<int> TracerMultiplier = new(4, Clamp(0, SlowTickMultiplierMax));
}

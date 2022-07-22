using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    public class ConfigDemo
    {
        [ConfigInfo("Playback tick multiplier rounded to the nearest tick. (0.5 = half speed, 2 = double speed)", save: true)]
        public readonly ConfigValue<double> PlaybackSpeed = new(1);
    }
}

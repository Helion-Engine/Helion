using Helion.Util.ConfigsNew.Values;
using static Helion.Util.ConfigsNew.Values.ConfigFilters;

namespace Helion.Util.ConfigsNew.Components
{
    public class ConfigConsole
    {
        [ConfigInfo("The number of messages the console buffer holds before discarding old ones.")]
        public readonly ConfigValue<int> MaxMessages = new(256, Greater(0));
    }
}

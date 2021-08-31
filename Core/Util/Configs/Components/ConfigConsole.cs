using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with the console.")]
    public class ConfigConsole
    {
        [ConfigInfo("The number of messages the console buffer holds before discarding old ones.")]
        public readonly ConfigValueInt MaxMessages = new(256);
    }
}

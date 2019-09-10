using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineConsoleConfig 
    {
        public readonly ConfigValue<int> MaxMessages = new ConfigValue<int>(256);
    }
}
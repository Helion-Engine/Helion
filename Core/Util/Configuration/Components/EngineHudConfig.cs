using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineHudConfig
    {
        public readonly ConfigValue<bool> FullStatusBar = new(true);
    }
}

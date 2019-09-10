using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineRenderMultisampleConfig
    {
        public readonly ConfigValue<bool> Enable = new ConfigValue<bool>(false);
        public readonly ConfigValue<int> Value = new ConfigValue<int>(4);
    }
}
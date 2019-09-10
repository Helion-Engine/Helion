using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineRenderAnisotropyConfig
    {
        public readonly ConfigValue<bool> Enable = new ConfigValue<bool>(true);
        public readonly ConfigValue<bool> UseMaxSupported = new ConfigValue<bool>(true);
        public readonly ConfigValue<double> Value = new ConfigValue<double>(8.0);
    }
}
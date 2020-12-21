using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineGameplayConfig
    {
        public readonly ConfigValue<bool> AutoAim = new ConfigValue<bool>(true);
        public readonly ConfigValue<float> MoveBob = new ConfigValue<float>(1.0f);
    }
}

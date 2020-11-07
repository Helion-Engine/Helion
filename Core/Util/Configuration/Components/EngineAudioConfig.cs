using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineAudioConfig
    {
        public readonly ConfigValue<float> Volume = new ConfigValue<float>(1.0f);
    }
}

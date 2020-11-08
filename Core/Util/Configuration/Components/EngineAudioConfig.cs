using Helion.Audio;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineAudioConfig
    {
        public readonly ConfigValue<float> Volume = new ConfigValue<float>(1.0f);
        public readonly ConfigValue<string> Device = new ConfigValue<string>(IAudioSystem.DefaultAudioDevice);
    }
}

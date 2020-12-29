using Helion.Audio;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineAudioConfig
    {
        public readonly ConfigValue<string> Device = new ConfigValue<string>(IAudioSystem.DefaultAudioDevice);
        public readonly ConfigValue<float> MusicVolume = new ConfigValue<float>(1.0f);
        public readonly ConfigValue<float> Volume = new ConfigValue<float>(1.0f);
    }
}

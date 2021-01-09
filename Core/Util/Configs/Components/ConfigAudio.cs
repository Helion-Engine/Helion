using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with audio presentation.")]
    public class ConfigAudio
    {
        [ConfigInfo("The main device to use for audio.")]
        public readonly ConfigValueString Device = new();

        [ConfigInfo("The volume of the music. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValueDouble MusicVolume = new(1.0);

        [ConfigInfo("The volume of the sounds. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValueDouble SoundVolume = new(1.0);

        [ConfigInfo("The volume of the sounds. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValueDouble Volume = new(1.0);
    }
}

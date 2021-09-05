﻿using Helion.Audio;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components
{
    public class ConfigAudio
    {
        [ConfigInfo("The main device to use for audio.")]
        public readonly ConfigValue<string> Device = new(IAudioSystem.DefaultAudioDevice);

        [ConfigInfo("The volume of the music. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValue<double> MusicVolume = new(1.0, ClampNormalized);

        [ConfigInfo("The volume of the sounds. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValue<double> SoundVolume = new(1.0, ClampNormalized);

        [ConfigInfo("The volume of the sounds. 0.0 is off, 1.0 is max.")]
        public readonly ConfigValue<double> Volume = new(1.0, ClampNormalized);
    }
}

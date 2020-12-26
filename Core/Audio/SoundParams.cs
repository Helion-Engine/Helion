using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio
{
    // TODO these values are all test values that sound ok for now
    public class SoundParams
    {
        public const float MaxVolume = 1.0f;
        public const float DefaultRolloff = 2.5f;
        public const float DefaultReference = 296.0f;
        public const float DefaultMaxDistance = 1752.0f;
        public const float DefaultRadius = 32.0f;

        public readonly ISoundSource SoundSource;
        public readonly bool Loop;
        public readonly float Volume;
        public Attenuation Attenuation { get; set; }
        public SoundInfo? SoundInfo { get; set; }

        public SoundParams(ISoundSource soundSource, bool loop = false, Attenuation attenuation = Attenuation.Default, float volume = MaxVolume)
        {
            SoundSource = soundSource;
            Attenuation = attenuation;
            Volume = volume;
            Loop = loop;
        }
    }
}

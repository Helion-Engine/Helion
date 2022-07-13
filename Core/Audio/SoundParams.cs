using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio;

// TODO these values are all test values that sound ok for now
public struct SoundParams
{
    public const float MaxVolume = 1.0f;
    public const float DefaultRolloff = 2.5f;
    public const float DefaultReference = 296.0f;
    public const float DefaultMaxDistance = 1752.0f;
    public const float DefaultRadius = 32.0f;

    public ISoundSource SoundSource { get; set; }
    public bool Loop { get; set; }
    public float Volume { get; set; }
    public Attenuation Attenuation { get; set; }
    public SoundType SoundType { get; set; }
    public SoundChannel Channel { get; set; }

    public SoundParams(ISoundSource soundSource, bool loop = false, Attenuation attenuation = Attenuation.Default, float volume = MaxVolume,
        SoundType type = SoundType.Default, SoundChannel channel = SoundChannel.Default)
    {
        SoundSource = soundSource;
        Attenuation = attenuation;
        Volume = volume;
        Loop = loop;
        SoundType = type;
        Channel = channel;
    }
}

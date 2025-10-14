using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio;

public struct AudioData(ISoundSource soundSource, SoundInfo soundInfo, SoundChannel channel, Attenuation attenuation,
    int priority, bool loop, bool relative, float volume, float attenuationFactor, float offsetSeconds, int gametick)
{

    /// <summary>
    /// The source object of the sound (e.g. entity, sector).
    /// </summary>
    public ISoundSource SoundSource = soundSource;

    /// <summary>
    /// SoundInfo source for this sound.
    /// </summary>
    public SoundInfo SoundInfo = soundInfo;

    /// <summary>
    /// The sound channel for this sound.
    /// </summary>
    public SoundChannel SoundChannelType = channel;

    /// <summary>
    /// The attenuation for this sound.
    /// </summary>
    public Attenuation Attenuation = attenuation;

    /// <summary>
    /// Priority for this sound, lower is higher priority.
    /// </summary>
    public int Priority = priority;

    /// <summary>
    /// If this sound should loop after completion.
    /// </summary>
    public bool Loop = loop;

    public bool Relative = relative;

    public float Volume = volume;

    public float AttenuationFactor = attenuationFactor;

    public float OffsetSeconds = offsetSeconds;

    public int GameTick = gametick;
}

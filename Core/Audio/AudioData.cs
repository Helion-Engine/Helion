using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio;

public class AudioData
{
    public AudioData(ISoundSource soundSource, SoundInfo soundInfo, SoundChannelType channel, Attenuation attenuation,
        int priority, bool loop)
    {
        SoundSource = soundSource;
        SoundInfo = soundInfo;
        SoundChannelType = channel;
        Attenuation = attenuation;
        Priority = priority;
        Loop = loop;
    }

    /// <summary>
    /// The source object of the sound (e.g. entity, sector).
    /// </summary>
    public ISoundSource SoundSource { get; set; }

    /// <summary>
    /// SoundInfo source for this sound.
    /// </summary>
    public SoundInfo SoundInfo { get; set; }

    /// <summary>
    /// The sound channel for this sound.
    /// </summary>
    public SoundChannelType SoundChannelType { get; set; }

    /// <summary>
    /// The attenuation for this sound.
    /// </summary>
    public Attenuation Attenuation { get; set; }

    /// <summary>
    /// Priority for this sound, lower is higher priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// If this sound should loop after completion.
    /// </summary>
    public bool Loop { get; set; }
}

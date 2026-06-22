using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util.Extensions;
using Helion.World.Entities;

namespace Helion.World.Sound;

public class DefaultSoundSource : ISoundSource
{
    public static readonly DefaultSoundSource Default = new();

    private IAudioSource? m_audioSource;
    private readonly Vec3D m_position;
    private readonly bool m_attenuate;

    public DefaultSoundSource()
    {
        m_position = Vec3D.Zero;
        m_attenuate = false;
    }

    public DefaultSoundSource(in Vec3D position)
    {
        m_position = position;
        m_attenuate = true;
    }

    public void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        m_audioSource = null;
    }

    public double GetDistanceSquaredFrom(Entity listenerEntity)
    {
        if (m_attenuate)
            return m_position.DistanceSquared(listenerEntity.Position);

        return 0.0;
    }

    public Vec3D? GetSoundPosition(Entity listenerEntity) => m_position;

    public Vec3D? GetSoundVelocity() => default;

    public void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {
        m_audioSource = audioSource;
    }

    public bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        clearedSound = m_audioSource;
        m_audioSource = null;
        return clearedSound != null;
    }

    public bool HasSound(string sound, SoundChannel channel)
    {
        return m_audioSource != null && m_audioSource.AudioDataRef().SoundInfo.Name.EqualsIgnoreCase(sound);
    }

    public bool CanMakeSound() => true;

    public float GetSoundRadius() => 32;
}

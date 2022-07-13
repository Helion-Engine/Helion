using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.World.Entities;

namespace Helion.World.Sound;

public interface ISoundSource
{
    void SoundCreated(IAudioSource audioSource, SoundChannel channel);
    IAudioSource? TryClearSound(string sound, SoundChannel channel);
    void ClearSound(IAudioSource audioSource, SoundChannel channel);
    double GetDistanceFrom(Entity listenerEntity);
    Vec3D? GetSoundPosition(Entity listenerEntity);
    Vec3D? GetSoundVelocity();
    bool CanMakeSound();
}

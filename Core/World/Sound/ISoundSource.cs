using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Entities;

namespace Helion.World.Sound;

public interface ISoundSource
{
    void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel);
    bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound);
    bool HasSound(string sound, SoundChannel channel);
    void ClearSound(IAudioSource audioSource, SoundChannel channel);
    double GetDistanceSquaredFrom(Entity listenerEntity);
    Vec3D? GetSoundPosition(Entity listenerEntity);
    Vec3D? GetSoundVelocity();
    bool CanMakeSound();
    float GetSoundRadius();
}

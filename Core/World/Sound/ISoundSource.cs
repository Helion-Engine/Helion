﻿using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Entities;

namespace Helion.World.Sound
{
    public interface ISoundSource
    {
        void SoundCreated(IAudioSource audioSource, SoundChannelType channel);
        IAudioSource? TryClearSound(string sound, SoundChannelType channel);
        void ClearSound(IAudioSource audioSource, SoundChannelType channel);
        double GetDistanceFrom(Entity listenerEntity);
        Vec3D? GetSoundPosition(Entity listenerEntity);
        Vec3D? GetSoundVelocity();
        bool CanAttenuate(SoundInfo soundInfo);
    }
}

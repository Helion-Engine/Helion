using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World.Entities;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Sound
{
    public class SoundManager : IDisposable, ITickable
    {
        /// <summary>
        /// The amount of sounds that are allowed to be playing at once. Any
        /// more sounds will be ignored.
        /// </summary>
        public const int MaxConcurrentSounds = 32;
        
        private readonly LinkedList<IAudioSource> m_playingSounds = new LinkedList<IAudioSource>();
        private readonly IAudioSourceManager m_audioManager;
        
        public SoundManager(IAudioSystem audioSystem)
        {
            m_audioManager = audioSystem.CreateContext();
        }

        ~SoundManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void Tick()
        {
            if (m_playingSounds.Empty())
                return;

            m_playingSounds.RemoveWhere(snd => snd.IsFinished()).ForEach(snd => snd.Dispose());
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_audioManager.Dispose();
        }

        public void CreateSoundOn(Entity entity, string sound, SoundChannelType channel)
        {
            Precondition((int)channel < EntitySoundChannels.MaxChannels, "ZDoom extra channel flags unsupported currently");

            // Because this (should) mutate the current playing sounds by doing
            // removal if a sound is on that channel via destruction, then the
            // sound
            entity.SoundChannels.DestroyChannelSound(channel);
            
            if (m_playingSounds.Count >= MaxConcurrentSounds)
                return;

            IAudioSource? audioSource = m_audioManager.Create(sound);
            if (audioSource == null)
                return;
            
            audioSource.Position = entity.Position.ToFloat();
            audioSource.Velocity = entity.Velocity.ToFloat();

            entity.SoundChannels.Add(audioSource, channel);
            m_playingSounds.AddLast(audioSource);

            audioSource.Play();
        }
    }
}
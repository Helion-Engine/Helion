using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
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
        private readonly IWorld m_world;
        
        public SoundManager(IWorld world, IAudioSystem audioSystem)
        {
            m_world = world;
            m_audioManager = audioSystem.CreateContext();
        }

        ~SoundManager()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public void Tick()
        {
            m_audioManager.SetListener(m_world.ListenerPosition, m_world.ListenerAngle, m_world.ListenerPitch);

            if (m_playingSounds.Empty())
                return;

            var node = m_playingSounds.First;

            while (node != null)
            {
                if (node.Value.IsFinished())
                {
                    node.Value.Dispose();
                    m_playingSounds.Remove(node);
                }
                else if (node.Value.SoundSource is Sector sector && sector.ActiveMoveSpecial is SectorMoveSpecial moveSpecial)
                {
                    node.Value.SetPosition(sector.GetSoundSource(m_world.ListenerEntity, moveSpecial.MoveData.SectorMoveType).ToFloat());
                }

                node = node.Next;
            }
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

        public void StopSoundsBySource(object source)
        {
            var node = m_playingSounds.First;

            while (node != null)
            {
                if (ReferenceEquals(source, node.Value.SoundSource))
                {
                    node.Value.Stop();
                    m_playingSounds.Remove(node);
                }

                node = node.Next;
            }
        }

        public void StopLoopSoundBySource(object source)
        {
            var node = m_playingSounds.First;

            while (node != null)
            {
                if (node.Value.Loop && ReferenceEquals(source, node.Value.SoundSource))
                {
                    node.Value.Stop();
                    m_playingSounds.Remove(node);
                }

                node = node.Next;
            }
        }

        public void CreateSoundOn(Entity entity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            // Because this (should) mutate the current playing sounds by doing
            // removal if a sound is on that channel via destruction, then the sound
            entity.SoundChannels.DestroyChannelSound(channel);

            StopSoundsBySource(entity);
            CreateSound(entity, entity.Position, entity.Velocity, sound, channel, soundParams);

            // TODO entity.SoundChannels.Add causes openAL to completely break
            //IAudioSource? audioSource = CreateSound(entity, entity.Position, entity.Velocity, sound, channel, soundParams);
            //if (audioSource == null)
            //    return;
            //entity.SoundChannels.Add(audioSource, channel);
        }

        public void CreateSoundAt(in Vec3D pos, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            CreateSound(null, pos, Vec3D.Zero, sound, channel, soundParams);
        }

        public IAudioSource? CreateSectorSound(Sector sector, SectorPlaneType type, string sound, SoundParams soundParams)
        {
            StopSoundsBySource(sector);
            return CreateSound(sector, sector.GetSoundSource(m_world.ListenerEntity, type), Vec3D.Zero, sound, SoundChannelType.Auto, soundParams);
        }

        private IAudioSource? CreateSound(object? source, in Vec3D pos, in Vec3D velocity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            Precondition((int)channel < EntitySoundChannels.MaxChannels, "ZDoom extra channel flags unsupported currently");

            if (m_playingSounds.Count >= MaxConcurrentSounds)
                return null;

            IAudioSource? audioSource = m_audioManager.Create(sound, soundParams);
            if (audioSource == null)
                return null;

            audioSource.SoundSource = source; 
            
            if (soundParams.Attenuation != Attenuation.None)
            {
                audioSource.SetPosition(pos.ToFloat());
                audioSource.SetVelocity(velocity.ToFloat());
            }

            m_playingSounds.AddLast(audioSource);
            audioSource.Play();

            return audioSource;
        }
    }
}
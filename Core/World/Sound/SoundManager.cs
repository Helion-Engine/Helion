using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Resource.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
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

        public readonly IAudioSourceManager AudioManager;

        private readonly LinkedList<IAudioSource> m_playingSounds = new LinkedList<IAudioSource>();
        private readonly IWorld m_world;
        private readonly SoundInfoDefinition m_soundInfo;
        private readonly List<IAudioSource> m_soundsToPlay = new List<IAudioSource>();

        public SoundManager(IWorld world, IAudioSystem audioSystem, SoundInfoDefinition soundInfo)
        {
            m_world = world;
            AudioManager = audioSystem.CreateContext();
            m_soundInfo = soundInfo;

            audioSystem.DeviceChanging += AudioSystem_DeviceChanging;
        }

        private void AudioSystem_DeviceChanging(object? sender, EventArgs e)
        {
            ClearSounds();
        }

        ~SoundManager()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void Tick()
        {
            AudioManager.SetListener(m_world.ListenerPosition, m_world.ListenerAngle, m_world.ListenerPitch);

            PlaySounds();

            if (m_playingSounds.Empty())
                return;

            LinkedListNode<IAudioSource>? node = m_playingSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.IsFinished())
                {
                    node.Value.Dispose();
                    m_playingSounds.Remove(node.Value);
                }
                else if (node.Value.SoundSource is Entity entity)
                {
                    node.Value.SetPosition(entity.Position.ToFloat());
                }
                else if (node.Value.SoundSource is Sector sector && sector.ActiveMoveSpecial is SectorMoveSpecial moveSpecial)
                {
                    node.Value.SetPosition(sector.GetSoundSource(m_world.ListenerEntity, moveSpecial.MoveData.SectorMoveType).ToFloat());
                }

                node = nextNode;
            }
        }

        public void ClearSounds()
        {
            m_playingSounds.ForEach(sound =>
            {
                sound.Stop();
                sound.Dispose();
            });
        }

        private void PlaySounds()
        {
            if (m_soundsToPlay.Count == 0)
                return;

            for (int i = 0; i < m_soundsToPlay.Count; i++)
                m_playingSounds.AddLast(m_soundsToPlay[i]);

            AudioManager.PlayGroup(m_soundsToPlay);
            m_soundsToPlay.Clear();
        }

        private void ReleaseUnmanagedResources()
        {
            AudioManager.Dispose();
        }

        private void StopSoundsBySource(object source)
        {
            LinkedListNode<IAudioSource>? node = m_playingSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (ReferenceEquals(source, node.Value.SoundSource))
                {
                    node.Value.Stop();
                    node.Value.Dispose();
                    m_playingSounds.Remove(node);
                }

                node = nextNode;
            }
        }

        public void StopLoopSoundBySource(object source)
        {
            LinkedListNode<IAudioSource>? node = m_playingSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.Loop && ReferenceEquals(source, node.Value.SoundSource))
                {
                    node.Value.Stop();
                    node.Value.Dispose();
                    m_playingSounds.Remove(node);
                }

                node = nextNode;
            }
        }

        public void CreateSoundOn(Entity entity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            // Because this (should) mutate the current playing sounds by doing
            // removal if a sound is on that channel via destruction, then the sound
            entity.SoundChannels.DestroyChannelSound(channel);
            CreateSound(entity, entity.Position, entity.Velocity, sound, channel, soundParams);

            // TODO entity.SoundChannels.Add causes openAL to completely break
            //IAudioSource? audioSource = CreateSound(entity, entity.Position, entity.Velocity, sound, channel, soundParams);
            //if (audioSource == null)
            //    return;
            //entity.SoundChannels.Add(audioSource, channel);
        }

        public IAudioSource? CreateSoundAt(in Vec3D pos, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            return CreateSound(null, pos, Vec3D.Zero, sound, channel, soundParams);
        }

        public IAudioSource? CreateSectorSound(Sector sector, SectorPlaneType type, string sound, SoundParams soundParams)
        {
            return CreateSound(sector, sector.GetSoundSource(m_world.ListenerEntity, type), Vec3D.Zero, sound, SoundChannelType.Auto, soundParams);
        }

        private IAudioSource? CreateSound(object? source, in Vec3D pos, in Vec3D velocity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            Precondition((int)channel < EntitySoundChannels.MaxChannels, "ZDoom extra channel flags unsupported currently");

            if (m_playingSounds.Count + m_soundsToPlay.Count >= MaxConcurrentSounds)
                return null;

            SoundInfo? soundInfo = GetSoundInfo(source, sound);

            if (soundInfo == null)
                return null;
            if (soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit)
                return null;

            soundParams.SoundInfo = soundInfo;
            IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, soundParams);
            if (audioSource == null)
                return null;

            audioSource.SoundInfo = soundInfo;
            audioSource.SoundSource = source;

            if (soundParams.Attenuation != Attenuation.None)
            {
                audioSource.SetPosition(pos.ToFloat());
                audioSource.SetVelocity(velocity.ToFloat());
            }

            if (audioSource.SoundSource != null)
                StopSoundsBySource(audioSource.SoundSource);
            AddSoundToPlay(audioSource);
            return audioSource;
        }

        private void AddSoundToPlay(IAudioSource audioSource)
        {
            for (int i = 0; i < m_soundsToPlay.Count; i++)
            {
                if (ReferenceEquals(audioSource.SoundSource, m_soundsToPlay[i].SoundSource))
                {
                    m_soundsToPlay[i].Dispose();
                    m_soundsToPlay[i] = audioSource;
                    return;
                }
            }

            m_soundsToPlay.Add(audioSource);
        }

        private SoundInfo? GetSoundInfo(object? source, string sound)
        {
            SoundInfo? soundInfo;

            if (source is Player player)
            {
                string playerSound = SoundInfoDefinition.GetPlayerSound(player, sound);
                soundInfo = m_soundInfo.Lookup(playerSound, m_world.Random);

                if (soundInfo != null)
                    return soundInfo;
            }

            soundInfo = m_soundInfo.Lookup(sound, m_world.Random);
            return soundInfo;
        }

        public int GetSoundCount(SoundInfo? soundInfo)
        {
            if (soundInfo == null)
                return 0;

            int count = 0;
            var node = m_playingSounds.First;

            while (node != null)
            {
                if (soundInfo.Equals(node.Value.SoundInfo))
                    count++;

                node = node.Next;
            }

            return count;
        }
    }
}
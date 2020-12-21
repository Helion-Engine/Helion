using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Resource.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Entities;
using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Special.SectorMovement;
using Helion.Worlds.Special.Specials;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Sound
{
    public class SoundManager : IDisposable, ITickable
    {
        /// <summary>
        /// The amount of sounds that are allowed to be playing at once. Any
        /// more sounds will be ignored.
        /// </summary>
        public const int MaxConcurrentSounds = 32;

        public readonly IAudioSourceManager AudioManager;

        private readonly LinkedList<IAudioSource> m_soundsToPlay = new();
        private readonly LinkedList<IAudioSource> m_playingSounds = new();
        private readonly LinkedList<IAudioSource> m_waitingLoopSounds = new();
        private readonly World m_world;
        private readonly SoundInfoManager m_soundInfoManager;

        public SoundManager(World world, IAudioSystem audioSystem, SoundInfoManager soundInfoManager)
        {
            m_world = world;
            AudioManager = audioSystem.CreateContext();
            m_soundInfoManager = soundInfoManager;

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
            UpdateWaitingLoopSounds();
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
                else if (node.Value.SoundSource != null)
                {
                    node.Value.Priority = GetPriority(node.Value.SoundSource, node.Value.SoundInfo, null, out Vec3D? position);
                    if (position != null)
                        node.Value.SetPosition(position.Value.ToFloat());
                }

                node = nextNode;
            }
        }

        public void StopLoopSoundBySource(object source)
        {
            StopSoundBySource(source, null, m_playingSounds, true);
            StopSoundBySource(source, null, m_waitingLoopSounds, true);
        }

        private void UpdateWaitingLoopSounds()
        {
            LinkedListNode<IAudioSource>? node = m_waitingLoopSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                node.Value.Priority = GetPriority(node.Value.SoundSource, node.Value.SoundInfo, null, out _);

                if (IsMaxSoundCount && (HitSoundLimit(node.Value.SoundInfo) || !BumpSoundByPriority(node.Value.Priority)))
                    return;

                m_soundsToPlay.AddLast(node.Value);
                m_waitingLoopSounds.Remove(node);

                node = nextNode;
            }
        }

        private Vec3D? GetSoundSourcePosition(object? soundSource)
        {
            if (soundSource is Entity entity)
                return entity.Position;
            if (soundSource is Sector sector && sector.ActiveMoveSpecial is SectorMoveSpecial moveSpecial)
                return sector.GetSoundSource(m_world.ListenerEntity, moveSpecial.MoveData.SectorMoveType);

            return null;
        }

        public void ClearSounds()
        {
            ClearSounds(m_soundsToPlay);
            ClearSounds(m_playingSounds);
            ClearSounds(m_waitingLoopSounds);
        }

        private static void ClearSounds(LinkedList<IAudioSource> audioSources)
        {
            audioSources.ForEach(sound =>
            {
                sound.Stop();
                sound.Dispose();
            });
        }

        private void PlaySounds()
        {
            if (m_soundsToPlay.Count == 0)
                return;

            LinkedListNode<IAudioSource>? node = m_soundsToPlay.First;
            while (node != null)
            {
                m_playingSounds.AddLast(node.Value);
                node = node.Next;
            }

            AudioManager.PlayGroup(m_soundsToPlay);
            m_soundsToPlay.Clear();
        }

        private void ReleaseUnmanagedResources()
        {
            AudioManager.Dispose();
        }

        private bool StopSoundsBySource(object? source, SoundInfo? soundInfo)
        {
            if (StopSoundBySource(source, soundInfo, m_soundsToPlay, false))
                return true;
            if (StopSoundBySource(source, soundInfo, m_playingSounds, false))
                return true;
            return false;
        }

        private bool StopSoundBySource(object? source, SoundInfo? soundInfo, LinkedList<IAudioSource> audioSources, bool loop)
        {
            if (source == null)
                return false;

            bool soundStopped = false;
            int priority = -1;
            LinkedListNode<IAudioSource>? node = audioSources.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.Loop == loop && ReferenceEquals(source, node.Value.SoundSource))
                {
                    if (priority == -1)
                        priority = GetPriority(source, soundInfo, null, out _);

                    if (node.Value.Priority < priority)
                    {
                        node = nextNode;
                        continue;
                    }

                    node.Value.Stop();
                    node.Value.Dispose();
                    audioSources.Remove(node);
                    soundStopped = true;
                }

                node = nextNode;
            }

            return soundStopped;
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
            SoundInfo? soundInfo = GetSoundInfo(source, sound);
            if (soundInfo == null)
                return null;

            int priority = GetPriority(source, soundInfo, soundParams, out _);
            bool waitLoopSound = false;

            // Check if this sound will remomve a sound by it's source first, then check bumping by priority
            if (IsMaxSoundCount && (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo) && !BumpSoundByPriority(priority))))
            {
                if (soundParams.Loop)
                    waitLoopSound = true;
                else
                    return null;
            }

            if (soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit)
                return null;

            // Don't attenuate sounds generated by the listener, otherwise movement can cause the sound to be off
            if (ReferenceEquals(source, m_world.ListenerEntity) || !CanAtennuate(source, soundInfo))
                soundParams.Attenuation = Attenuation.None;
            soundParams.SoundInfo = soundInfo;
            IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, soundParams);
            if (audioSource == null)
                return null;

            audioSource.Priority = priority;
            audioSource.SoundInfo = soundInfo;
            audioSource.SoundSource = source;

            if (soundParams.Attenuation != Attenuation.None)
            {
                audioSource.SetPosition(pos.ToFloat());
                audioSource.SetVelocity(velocity.ToFloat());
            }

            if (waitLoopSound)
                m_waitingLoopSounds.AddLast(audioSource);
            else
                m_soundsToPlay.AddLast(audioSource);
            return audioSource;
        }

        private bool HitSoundLimit(SoundInfo? soundInfo)
        {
            if (soundInfo == null)
                return true;

            return soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit;
        }

        private bool IsMaxSoundCount => m_soundsToPlay.Count + m_playingSounds.Count >= MaxConcurrentSounds;

        private bool BumpSoundByPriority(int priority)
        {
            if (BumpSoundByPriority(priority, m_soundsToPlay))
                return true;
            if (BumpSoundByPriority(priority, m_playingSounds))
                return true;

            return false;
        }

        private bool BumpSoundByPriority(int priority, LinkedList<IAudioSource> audioSources)
        {
            int lowestPriority = 0;
            LinkedListNode<IAudioSource>? lowestPriorityNode = null;
            LinkedListNode<IAudioSource>? node = audioSources.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.Priority > lowestPriority)
                {
                    lowestPriorityNode = node;
                    lowestPriority = node.Value.Priority;
                }

                node = nextNode;
            }

            if (lowestPriorityNode != null && priority < lowestPriority)
            {
                lowestPriorityNode.Value.Stop();

                if (lowestPriorityNode.Value.Loop)
                    m_waitingLoopSounds.AddLast(lowestPriorityNode.Value);
                else
                    lowestPriorityNode.Value.Dispose();

                audioSources.Remove(lowestPriorityNode);
                return true;
            }

            return false;
        }

        private int GetPriority(object? soundSource, SoundInfo? soundInfo, SoundParams? soundParams, out Vec3D? position)
        {
            position = null;
            // Sounds from the listener are top priority.
            // Sounds that do not attenuate are next, then prioritize sounds by distance.
            if (ReferenceEquals(soundSource, m_world.ListenerEntity))
                return 0;

            if (soundParams != null && soundParams.Attenuation == Attenuation.None || !CanAtennuate(soundSource, soundInfo))
                return 1;

            position = GetSoundSourcePosition(soundSource);
            if (position != null)
                return (int)position.Value.Distance(m_world.ListenerPosition) + 1;

            return int.MaxValue;
        }

        private static bool CanAtennuate(object? soundSource, SoundInfo? soundInfo)
        {
            if (soundInfo != null && soundSource is Entity entity && entity.Flags.Boss &&
                (soundInfo.Name == entity.Definition.Properties.SeeSound || soundInfo.Name == entity.Definition.Properties.DeathSound))
                return false;

            return true;
        }

        private SoundInfo? GetSoundInfo(object? source, string sound)
        {
            SoundInfo? soundInfo;

            if (source is Player player)
            {
                string playerSound = SoundInfoManager.GetPlayerSound(player, sound);
                soundInfo = m_soundInfoManager.Lookup(playerSound, m_world.Random);

                if (soundInfo != null)
                    return soundInfo;
            }

            soundInfo = m_soundInfoManager.Lookup(sound, m_world.Random);
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
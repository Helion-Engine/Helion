using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
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

        private readonly LinkedList<IAudioSource> m_soundsToPlay = new LinkedList<IAudioSource>();
        private readonly LinkedList<IAudioSource> m_playingSounds = new LinkedList<IAudioSource>();
        private readonly LinkedList<IAudioSource> m_waitingLoopSounds = new LinkedList<IAudioSource>();
        private readonly IWorld m_world;
        private readonly SoundInfoDefinition m_soundInfo;
        
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
                else if (node.Value.AudioData.SoundSource != null)
                {
                    Vec3D? position = node.Value.AudioData.SoundSource.GetSoundPosition(m_world.ListenerEntity);
                    if (position != null)
                        node.Value.SetPosition(position.Value.ToFloat());
                }

                node = nextNode;
            }
        }

        public void StopSoundBySource(ISoundSource source, SoundChannelType channel, string sound)
        {
            IAudioSource? stoppedSound = source.TryClearSound(sound, channel);
            if (stoppedSound != null)
            {
                StopSound(stoppedSound, m_soundsToPlay);
                StopSound(stoppedSound, m_playingSounds);
                StopSound(stoppedSound, m_waitingLoopSounds);
            }
        }

        private void StopSound(IAudioSource audioSource, LinkedList<IAudioSource> audioSources)
        {
            LinkedListNode<IAudioSource>? node = audioSources.First;
            while (node != null)
            {
                if (ReferenceEquals(audioSource, node.Value))
                {
                    node.Value.Stop();
                    node.Value.Dispose();
                    audioSources.Remove(audioSource);
                }
                node = node.Next;
            }
        }

        private void UpdateWaitingLoopSounds()
        {
            LinkedListNode<IAudioSource>? node = m_waitingLoopSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                double distance = GetSoundSourceDistance(node.Value.AudioData.SoundSource);

                if (IsMaxSoundCount && (HitSoundLimit(node.Value.AudioData.SoundInfo) || !BumpSoundByPriority(node.Value.AudioData.Priority, distance)))
                    return;

                m_soundsToPlay.AddLast(node.Value);
                m_waitingLoopSounds.Remove(node);

                node = nextNode;
            }
        }

        private double GetSoundSourceDistance(ISoundSource? soundSource)
        {
            if (soundSource == null)
                return 0.0;

            return soundSource.GetDistanceFrom(m_world.ListenerEntity);
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
                sound.AudioData.SoundSource?.ClearSound(sound, sound.AudioData.SoundChannelType);
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

        private bool StopSoundsBySource(ISoundSource? source, SoundInfo? soundInfo, SoundChannelType channel)
        {
            if (StopSoundBySource(source, soundInfo, channel, m_soundsToPlay))
                return true;
            if (StopSoundBySource(source, soundInfo, channel, m_playingSounds))
                return true;
            return false;
        }

        private bool StopSoundBySource(ISoundSource? source, SoundInfo? soundInfo, SoundChannelType channel, LinkedList<IAudioSource> audioSources, string? sound = null)
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
                if (ReferenceEquals(source, node.Value.AudioData.SoundSource))
                {
                    if (sound != null && node.Value.AudioData.SoundInfo != null && node.Value.AudioData.SoundInfo.Name != sound)
                    {
                        node = nextNode;
                        continue;
                    }

                    if (channel != node.Value.AudioData.SoundChannelType)
                    {
                        node = nextNode;
                        continue;
                    }

                    if (priority == -1 && soundInfo != null)
                        priority = GetPriority(source, soundInfo, null);

                    if (node.Value.AudioData.Priority < priority)
                    {
                        node = nextNode;
                        continue;
                    }

                    node.Value.AudioData.SoundSource?.ClearSound(node.Value, node.Value.AudioData.SoundChannelType);
                    node.Value.Stop();
                    node.Value.Dispose();
                    audioSources.Remove(node);
                    soundStopped = true;
                }

                node = nextNode;
            }

            return soundStopped;
        }

        public void CreateSoundOn(ISoundSource soundSource, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            CreateSound(soundSource, soundSource.GetSoundPosition(m_world.ListenerEntity), soundSource.GetSoundVelocity(), sound, channel, soundParams);
        }

        public IAudioSource? CreateSoundAt(in Vec3D pos, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            return CreateSound(null, pos, Vec3D.Zero, sound, channel, soundParams);
        }

        public IAudioSource? CreateSectorSound(Sector sector, SectorPlaneType type, string sound, SoundParams soundParams)
        {
            return CreateSound(sector, sector.GetSoundSource(m_world.ListenerEntity, type), Vec3D.Zero, sound, SoundChannelType.Auto, soundParams);
        }

        private IAudioSource? CreateSound(ISoundSource? source, in Vec3D? pos, in Vec3D? velocity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            Precondition((int)channel < Entity.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
            SoundInfo? soundInfo = GetSoundInfo(source, sound);
            if (soundInfo == null)
                return null;

            int priority = GetPriority(source, soundInfo, soundParams);
            double distance = GetSoundSourceDistance(source);
            bool waitLoopSound = false;

            // Check if this sound will remomve a sound by it's source first, then check bumping by priority
            if (IsMaxSoundCount && (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo, channel) && !BumpSoundByPriority(priority, distance))))
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
            AudioData audioData = new AudioData(source, soundInfo, channel, priority, soundParams.Loop);
            IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, audioData, soundParams);
            if (audioSource == null)
                return null;

            if (soundParams.Attenuation != Attenuation.None)
            {
                if (pos != null)
                    audioSource.SetPosition(pos.Value.ToFloat());
                if (velocity != null)
                    audioSource.SetVelocity(velocity.Value.ToFloat());
            }

            if (waitLoopSound)
            {
                m_waitingLoopSounds.AddLast(audioSource);
            }
            else
            {
                StopSoundsBySource(source, soundInfo, channel);
                m_soundsToPlay.AddLast(audioSource);
            }

            source?.SoundCreated(audioSource, channel);

            return audioSource;
        }

        private bool HitSoundLimit(SoundInfo? soundInfo)
        {
            if (soundInfo == null)
                return true;

            return soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit;
        }

        private bool IsMaxSoundCount => m_soundsToPlay.Count + m_playingSounds.Count >= MaxConcurrentSounds;

        private bool BumpSoundByPriority(int priority, double distance)
        {
            if (BumpSoundByPriority(priority, distance, m_soundsToPlay))
                return true;
            if (BumpSoundByPriority(priority, distance, m_playingSounds))
                return true;

            return false;
        }

        private bool BumpSoundByPriority(int priority, double distance, LinkedList<IAudioSource> audioSources)
        {
            int lowestPriority = 0;
            double farthestDistance = 0;
            LinkedListNode<IAudioSource>? lowestPriorityNode = null;
            LinkedListNode<IAudioSource>? node = audioSources.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.AudioData.Priority > lowestPriority)
                {
                    double checkDistance = GetSoundSourceDistance(node.Value.AudioData.SoundSource);
                    if (checkDistance > farthestDistance)
                    {
                        lowestPriorityNode = node;
                        lowestPriority = node.Value.AudioData.Priority;
                        farthestDistance = checkDistance;
                    }
                }

                node = nextNode;
            }

            if (lowestPriorityNode != null && priority < lowestPriority && distance < farthestDistance)
            {
                lowestPriorityNode.Value.Stop();

                if (lowestPriorityNode.Value.AudioData.Loop)
                    m_waitingLoopSounds.AddLast(lowestPriorityNode.Value);
                else
                    lowestPriorityNode.Value.Dispose();

                audioSources.Remove(lowestPriorityNode);
                return true;
            }

            return false;
        }

        private int GetPriority(ISoundSource? soundSource, SoundInfo? soundInfo, SoundParams? soundParams)
        {
            // Sounds from the listener are top priority.
            // Sounds that do not attenuate are next, then prioritize sounds by the type the entity is producing.
            if (ReferenceEquals(soundSource, m_world.ListenerEntity))
                return 0;

            if (soundParams != null && soundParams.Attenuation == Attenuation.None || !CanAtennuate(soundSource, soundInfo))
                return 1;

            if (soundInfo != null && soundSource is Entity entity && entity.Flags.Monster)
            {
                if (soundInfo.Name == entity.Properties.PainSound)
                    return 3;
                else if (soundInfo.Name == entity.Properties.SeeSound)
                    return 4;
                else if (soundInfo.Name == entity.Properties.ActiveSound)
                    return 5;
            }

            return 2;
        }

        private static bool CanAtennuate(ISoundSource? soundSource, SoundInfo? soundInfo)
        {
            if (soundSource == null || soundInfo == null)
                return true;

            return soundSource.CanAttenuate(soundInfo);
        }

        private SoundInfo? GetSoundInfo(ISoundSource? source, string sound)
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
                if (soundInfo.Equals(node.Value.AudioData.SoundInfo))
                    count++;

                node = node.Next;
            }

            return count;
        }
    }
}
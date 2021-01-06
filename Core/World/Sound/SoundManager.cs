using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Entities.Players;
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

        // The sounds that are generated in the same gametic that are waiting to be played as a group
        private readonly LinkedList<IAudioSource> m_soundsToPlay = new LinkedList<IAudioSource>();

        // The sounds that are currently playing
        private readonly LinkedList<IAudioSource> m_playingSounds = new LinkedList<IAudioSource>();

        // Looping sounds that are saved but not currently playing.
        // It's either too far away to hear yet or was bumped by a higher priority sound.
        // These sounds are continually checked if they can be added in to play.
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
                else
                {
                    double distance = node.Value.AudioData.SoundSource.GetDistanceFrom(m_world.ListenerEntity);
                    if (!CheckDistance(distance, node.Value.AudioData.Attenuation))
                    {
                        node.Value.Stop();
                        m_playingSounds.Remove(node);

                        if (ShouldDisposeBumpedSound(node.Value))
                            node.Value.Dispose();
                    }
                    else
                    {
                        Vec3D? position = node.Value.AudioData.SoundSource.GetSoundPosition(m_world.ListenerEntity);
                        if (position != null)
                            node.Value.SetPosition(position.Value.ToFloat());
                    }
                }

                node = nextNode;
            }
        }

        public void Pause()
        {
            LinkedListNode<IAudioSource>? node = m_playingSounds.First;
            while (node != null)
            {
                node.Value.Pause();
                node = node.Next;
            }
        }

        public void Resume()
        {
            LinkedListNode<IAudioSource>? node = m_playingSounds.First;
            while (node != null)
            {
                node.Value.Play();
                node = node.Next;
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
                double distance = node.Value.AudioData.SoundSource.GetDistanceFrom(m_world.ListenerEntity);

                if (!CheckDistance(distance, node.Value.AudioData.Attenuation))
                {
                    node = nextNode;
                    continue;
                }

                if (IsMaxSoundCount && (HitSoundLimit(node.Value.AudioData.SoundInfo) || !BumpSoundByPriority(node.Value.AudioData.Priority, distance)))
                    return;

                m_soundsToPlay.AddLast(node.Value);
                m_waitingLoopSounds.Remove(node);

                node = nextNode;
            }
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
                sound.AudioData.SoundSource.ClearSound(sound, sound.AudioData.SoundChannelType);
                sound.Stop();
                sound.Dispose();
            });

            audioSources.Clear();
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

        private bool StopSoundsBySource(ISoundSource source, SoundInfo? soundInfo, SoundChannelType channel)
        {
            if (StopSoundBySource(source, soundInfo, channel, m_soundsToPlay))
                return true;
            if (StopSoundBySource(source, soundInfo, channel, m_playingSounds))
                return true;
            return false;
        }

        private bool StopSoundBySource(ISoundSource source, SoundInfo? soundInfo, SoundChannelType channel, LinkedList<IAudioSource> audioSources, string? sound = null)
        {
            if (source == null)
                return false;

            bool soundStopped = false;
            int priority = GetPriority(source, soundInfo, null);
            LinkedListNode<IAudioSource>? node = audioSources.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (ReferenceEquals(source, node.Value.AudioData.SoundSource))
                {
                    if (sound != null && node.Value.AudioData.SoundInfo.Name != sound ||
                        channel != node.Value.AudioData.SoundChannelType ||
                        node.Value.AudioData.Priority < priority)
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

        public IAudioSource? CreateSoundOn(ISoundSource soundSource, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            return CreateSound(soundSource, soundSource.GetSoundPosition(m_world.ListenerEntity), soundSource.GetSoundVelocity(), sound, channel, soundParams);
        }

        private IAudioSource? CreateSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, string sound, SoundChannelType channel, SoundParams soundParams)
        {
            Precondition((int)channel < Entity.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
            SoundInfo? soundInfo = GetSoundInfo(source, sound);
            if (soundInfo == null)
                return null;

            // Don't attenuate sounds generated by the listener, otherwise movement can cause the sound to be off
            if (ReferenceEquals(source, m_world.ListenerEntity) || !CanAtennuate(source, soundInfo))
                soundParams.Attenuation = Attenuation.None;

            bool waitLoopSound = false;
            double distance = source.GetDistanceFrom(m_world.ListenerEntity);
            int priority = GetPriority(source, soundInfo, soundParams);
            if (!CheckDistance(distance, soundParams.Attenuation) || SoundPriorityTooLow(source, channel, soundInfo, distance, priority))
            {
                if (soundParams.Loop)
                    waitLoopSound = true;
                else
                    return null;
            }

            if (HitSoundLimit(soundInfo) && !StopSoundsBySource(source, soundInfo, channel))
                return null;

            soundParams.SoundInfo = soundInfo;
            AudioData audioData = new AudioData(source, soundInfo, channel, soundParams.Attenuation, priority, soundParams.Loop);
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

        private bool SoundPriorityTooLow(ISoundSource source, SoundChannelType channel, SoundInfo soundInfo, double distance, int priority)
        {
            // Check if this sound will remomve a sound by it's source first, then check bumping by priority
            return IsMaxSoundCount && (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo, channel) && !BumpSoundByPriority(priority, distance)));
        }

        private bool HitSoundLimit(SoundInfo soundInfo)
        {
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
                    double checkDistance = node.Value.AudioData.SoundSource.GetDistanceFrom(m_world.ListenerEntity);
                    if (checkDistance > farthestDistance)
                    {
                        lowestPriorityNode = node;
                        lowestPriority = node.Value.AudioData.Priority;
                        farthestDistance = checkDistance;
                    }
                }

                node = nextNode;
            }

            if (lowestPriorityNode != null && priority <= lowestPriority && distance < farthestDistance)
            {
                lowestPriorityNode.Value.Stop();

                if (ShouldDisposeBumpedSound(lowestPriorityNode.Value))
                    lowestPriorityNode.Value.Dispose();

                audioSources.Remove(lowestPriorityNode);
                return true;
            }

            return false;
        }

        private bool ShouldDisposeBumpedSound(IAudioSource audioSource)
        {
            if (audioSource.AudioData.Loop)
            {
                m_waitingLoopSounds.AddLast(audioSource);
                return false;
            }

            return true;
        }

        private int GetPriority(ISoundSource soundSource, SoundInfo? soundInfo, SoundParams? soundParams)
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

        private static bool CheckDistance(double distance, Attenuation attenuation)
        {
            if (attenuation != Attenuation.None && distance > Constants.MaxSoundDistance)
                return false;

            return true;
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
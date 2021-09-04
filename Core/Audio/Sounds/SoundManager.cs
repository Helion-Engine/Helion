using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Sound;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Sounds
{
    public class SoundManager : IDisposable
    {
        /// <summary>
        /// The amount of sounds that are allowed to be playing at once. Any
        /// more sounds will be ignored.
        /// </summary>
        public const int MaxConcurrentSounds = 32;

        public readonly IAudioSourceManager AudioManager;
        protected readonly ArchiveCollection ArchiveCollection;
        private readonly IRandom m_random = new TrueRandom();
        private readonly IAudioSystem m_audioSystem;

        // The sounds that are currently playing
        protected readonly LinkedList<IAudioSource> PlayingSounds = new();
        
        // The sounds that are generated in the same gametic that are waiting to be played as a group
        private readonly LinkedList<IAudioSource> m_soundsToPlay = new();

        // Looping sounds that are saved but not currently playing.
        // It's either too far away to hear yet or was bumped by a higher priority sound.
        // These sounds are continually checked if they can be added in to play.
        private readonly LinkedList<IAudioSource> m_waitingLoopSounds = new();
        
        public SoundManager(IAudioSystem audioSystem, ArchiveCollection archiveCollection)
        {
            AudioManager = audioSystem.CreateContext();
            ArchiveCollection = archiveCollection;
            m_audioSystem = audioSystem;

            audioSystem.DeviceChanging += AudioSystem_DeviceChanging;
        }

        private void AudioSystem_DeviceChanging(object? sender, EventArgs e)
        {
            ClearSounds();
            AudioManager.DeviceChanging();
        }

        ~SoundManager()
        {
            ReleaseUnmanagedResources();
            FailedToDispose(this);
        }

        public void Dispose()
        {
            ClearSounds();
            ReleaseUnmanagedResources();

            m_audioSystem.DeviceChanging -= AudioSystem_DeviceChanging;

            GC.SuppressFinalize(this);
        }
        
        private void ReleaseUnmanagedResources()
        {
            AudioManager.Dispose();
        }

        public void Pause()
        {
            LinkedListNode<IAudioSource>? node = PlayingSounds.First;
            while (node != null)
            {
                node.Value.Pause();
                node = node.Next;
            }
        }

        public void Resume()
        {
            LinkedListNode<IAudioSource>? node = PlayingSounds.First;
            while (node != null)
            {
                node.Value.Play();
                node = node.Next;
            }
        }

        public void StopSoundBySource(ISoundSource source, SoundChannelType channel, string sound)
        {
            IAudioSource? stoppedSound = source.TryClearSound(sound, channel);
            if (stoppedSound == null) 
                return;
            
            StopSound(stoppedSound, m_soundsToPlay);
            StopSound(stoppedSound, PlayingSounds);
            StopSound(stoppedSound, m_waitingLoopSounds);
        }

        protected void StopSound(IAudioSource audioSource, LinkedList<IAudioSource> audioSources)
        {
            LinkedListNode<IAudioSource>? node = audioSources.First;
            while (node != null)
            {
                if (ReferenceEquals(audioSource, node.Value))
                {
                    node.Value.Stop();
                    DataCache.Instance.FreeAudioSource(node.Value);
                    audioSources.Remove(audioSource);
                }
                node = node.Next;
            }
        }
        
        protected virtual double GetDistance(ISoundSource soundSource) => 0;
        
        protected virtual IRandom GetRandom() => m_random;

        protected void UpdateWaitingLoopSounds()
        {
            LinkedListNode<IAudioSource>? node = m_waitingLoopSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                double distance = GetDistance(node.Value.AudioData.SoundSource);

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
            ClearSounds(PlayingSounds);
            ClearSounds(m_waitingLoopSounds);
        }

        private static void ClearSounds(LinkedList<IAudioSource> audioSources)
        {
            foreach (IAudioSource sound in audioSources)
            {
                sound.Stop();
                DataCache.Instance.FreeAudioSource(sound);
            }

            audioSources.Clear();
        }

        protected void PlaySounds()
        {
            if (m_soundsToPlay.Count == 0)
                return;

            LinkedListNode<IAudioSource>? node = m_soundsToPlay.First;
            while (node != null)
            {
                PlayingSounds.AddLast(node.Value);
                node = node.Next;
            }

            AudioManager.PlayGroup(m_soundsToPlay);
            m_soundsToPlay.Clear();
        }
        
        private bool StopSoundsBySource(ISoundSource source, SoundInfo? soundInfo, SoundChannelType channel)
        {
            // Always try to stop looping sounds that are waiting to be in range
            // This does not free up a sound if the limit has been hit
            StopSoundBySource(source, soundInfo, channel, m_waitingLoopSounds);

            if (StopSoundBySource(source, soundInfo, channel, m_soundsToPlay))
                return true;
            if (StopSoundBySource(source, soundInfo, channel, PlayingSounds))
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

                    node.Value.Stop();
                    DataCache.Instance.FreeAudioSource(node.Value);
                    audioSources.Remove(node);
                    soundStopped = true;
                }

                node = nextNode;
            }

            return soundStopped;
        }

        public virtual IAudioSource? PlayStaticSound(string sound)
        {
            ISoundSource soundSource = DefaultSoundSource.Default;
            SoundParams soundParams = new(soundSource, attenuation: Attenuation.None);
            return CreateSound(soundSource, Vec3D.Zero, Vec3D.Zero, sound, SoundChannelType.Auto, soundParams);
        }

        protected IAudioSource? CreateSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, string sound, 
            SoundChannelType channel, SoundParams soundParams)
        {
            Precondition((int)channel < Entity.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
            SoundInfo? soundInfo = GetSoundInfo(source, sound);
            if (soundInfo == null)
            {
                DataCache.Instance.FreeSoundParams(soundParams);
                return null;
            }

            AttenuateIfNeeded(source, soundInfo, soundParams);

            bool waitLoopSound = false;
            double distance = GetDistance(source);
            int priority = GetPriority(source, soundInfo, soundParams);
            bool soundTooFar = !CheckDistance(distance, soundParams.Attenuation);
            if (soundTooFar || SoundPriorityTooLow(source, channel, soundInfo, distance, priority))
            {
                if (soundTooFar)
                    StopSoundsBySource(source, soundInfo, channel);

                if (soundParams.Loop)
                {
                    waitLoopSound = true;
                }
                else
                {
                    DataCache.Instance.FreeSoundParams(soundParams);
                    return null;
                }
            }

            if (HitSoundLimit(soundInfo) && !StopSoundsBySource(source, soundInfo, channel))
            {
                DataCache.Instance.FreeSoundParams(soundParams);
                return null;
            }

            soundParams.SoundInfo = soundInfo;
            AudioData audioData = DataCache.Instance.GetAudioData(source, soundInfo, channel, soundParams.Attenuation, priority, soundParams.Loop);
            IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, audioData, soundParams);
            if (audioSource == null)
            {
                DataCache.Instance.FreeSoundParams(soundParams);
                return null;
            }

            if (soundParams.Attenuation != Attenuation.None)
            {
                if (pos != null)
                    audioSource.SetPosition(pos.Value.Float);
                if (velocity != null)
                    audioSource.SetVelocity(velocity.Value.Float);
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

        protected virtual void AttenuateIfNeeded(ISoundSource source, SoundInfo info, SoundParams soundParams)
        {
            // To be overridden if needed.
        }

        private bool SoundPriorityTooLow(ISoundSource source, SoundChannelType channel, SoundInfo soundInfo, double distance, int priority)
        {
            // Check if this sound will remove a sound by it's source first, then check bumping by priority
            return IsMaxSoundCount && (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo, channel) && !BumpSoundByPriority(priority, distance)));
        }

        private bool HitSoundLimit(SoundInfo soundInfo)
        {
            return soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit;
        }

        private bool IsMaxSoundCount => m_soundsToPlay.Count + PlayingSounds.Count >= MaxConcurrentSounds;

        private bool BumpSoundByPriority(int priority, double distance)
        {
            if (BumpSoundByPriority(priority, distance, m_soundsToPlay))
                return true;
            if (BumpSoundByPriority(priority, distance, PlayingSounds))
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
                    double checkDistance = GetDistance(node.Value.AudioData.SoundSource);
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
                    DataCache.Instance.FreeAudioSource(lowestPriorityNode.Value);

                audioSources.Remove(lowestPriorityNode);
                return true;
            }

            return false;
        }

        protected bool ShouldDisposeBumpedSound(IAudioSource audioSource)
        {
            if (audioSource.AudioData.Loop)
            {
                m_waitingLoopSounds.AddLast(audioSource);
                return false;
            }

            return true;
        }

        protected virtual int GetPriority(ISoundSource soundSource, SoundInfo? soundInfo, SoundParams? soundParams)
        {
            if (soundParams is { Attenuation: Attenuation.None } || !CanAttenuate(soundSource, soundInfo))
                return 1;

            if (soundInfo != null && soundSource is Entity entity && !entity.IsPlayer)
            {
                if (soundInfo.Name.Equals(entity.Properties.PainSound, StringComparison.OrdinalIgnoreCase))
                    return 3;
                if (soundInfo.Name.Equals(entity.Properties.SeeSound, StringComparison.OrdinalIgnoreCase))
                    return 4;
                if (soundInfo.Name.Equals(entity.Properties.ActiveSound, StringComparison.OrdinalIgnoreCase))
                    return 5;
            }

            return 2;
        }

        protected static bool CanAttenuate(ISoundSource? soundSource, SoundInfo? soundInfo)
        {
            if (soundSource == null || soundInfo == null)
                return true;

            return soundSource.CanAttenuate(soundInfo);
        }

        protected static bool CheckDistance(double distance, Attenuation attenuation)
        {
            return attenuation == Attenuation.None || distance <= Constants.MaxSoundDistance;
        }

        protected virtual SoundInfo? GetSoundInfo(ISoundSource? source, string sound)
        {
            return ArchiveCollection.Definitions.SoundInfo.Lookup(sound, GetRandom());
        }

        public int GetSoundCount(SoundInfo? soundInfo)
        {
            if (soundInfo == null)
                return 0;

            int count = 0;
            var node = PlayingSounds.First;

            while (node != null)
            {
                if (soundInfo.Equals(node.Value.AudioData.SoundInfo))
                    count++;

                node = node.Next;
            }

            return count;
        }

        public virtual void Update()
        {
            // Note: We do not set the position here since everything should be
            // attenuated globally.
            UpdateWaitingLoopSounds();
            PlaySounds();

            if (PlayingSounds.Empty())
                return;

            LinkedListNode<IAudioSource>? node = PlayingSounds.First;
            LinkedListNode<IAudioSource>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.IsFinished())
                {
                    DataCache.Instance.FreeAudioSource(node.Value);
                    PlayingSounds.Remove(node.Value);
                }

                node = nextNode;
            }
        }
    }
}

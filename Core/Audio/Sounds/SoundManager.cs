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

namespace Helion.Audio.Sounds;

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
    private readonly LinkedList<WaitingSound> m_waitingLoopSounds = new();

    public SoundManager(IAudioSystem audioSystem, ArchiveCollection archiveCollection)
    {
        AudioManager = audioSystem.CreateContext();
        ArchiveCollection = archiveCollection;
        m_audioSystem = audioSystem;

        audioSystem.DeviceChanging += AudioSystem_DeviceChanging;
    }

    public IAudioSource? FindBySource(object source)
    {
        IAudioSource? audioSource = FindBySource(source, m_soundsToPlay);
        if (audioSource != null)
            return audioSource;

        return FindBySource(source, PlayingSounds);
    }

    private static IAudioSource? FindBySource(object source, LinkedList<IAudioSource> audioSources)
    {
        var node = audioSources.First;
        while (node != null)
        {
            if (ReferenceEquals(source, node.Value.AudioData.SoundSource))
                return node.Value;
            node = node.Next;
        }

        return null;
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

    public void CacheSound(string name)
    {
        SoundInfo? soundInfo = GetSoundInfo(null, name);
        if (soundInfo != null)
            AudioManager.CacheSound(soundInfo.EntryName);
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
                ArchiveCollection.DataCache.FreeAudioSource(node.Value);
                audioSources.Remove(audioSource);
            }
            node = node.Next;
        }
    }

    protected void StopSound(IAudioSource audioSource, LinkedList<WaitingSound> waitingSounds)
    {
        LinkedListNode<WaitingSound>? node = waitingSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (ReferenceEquals(audioSource.AudioData.SoundSource, node.Value.SoundSource))
                waitingSounds.Remove(node);
            node = nextNode;
        }
    }

    protected virtual double GetDistance(ISoundSource soundSource) => 0;

    protected virtual IRandom GetRandom() => m_random;

    protected void UpdateWaitingLoopSounds()
    {
        LinkedListNode<WaitingSound>? node = m_waitingLoopSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            double distance = GetDistance(node.Value.SoundSource);

            if (!CheckDistance(distance, node.Value.SoundParams.Attenuation))
            {
                node = nextNode;
                continue;
            }

            if (IsMaxSoundCount && (HitSoundLimit(node.Value.SoundInfo) || !BumpSoundByPriority(node.Value.Priority, distance)))
                return;

            var value = node.Value;
            var audio = CreateSound(value.SoundSource, value.Position, value.Velocity, value.SoundInfo.Name, value.Channel, value.SoundParams, out _);
            // If the sound was successfully created then remove from waiting loop sound list. Also check it wasn't already removed.
            if (audio != null && node.List == m_waitingLoopSounds)
                m_waitingLoopSounds.Remove(node);

            node = nextNode;
        }
    }

    public void ClearSounds()
    {
        ClearSounds(m_soundsToPlay);
        ClearSounds(PlayingSounds);
        m_waitingLoopSounds.Clear();
    }

    private void ClearSounds(LinkedList<IAudioSource> audioSources)
    {
        foreach (IAudioSource sound in audioSources)
        {
            sound.Stop();
            ArchiveCollection.DataCache.FreeAudioSource(sound);
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

    private bool StopSoundsBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, SoundChannelType channel)
    {
        // Always try to stop looping sounds that are waiting to be in range
        // This does not free up a sound if the limit has been hit
        StopSoundBySource(source, soundInfo, soundParams, channel, m_waitingLoopSounds);

        if (StopSoundBySource(source, soundInfo, soundParams, channel, m_soundsToPlay))
            return true;
        if (StopSoundBySource(source, soundInfo, soundParams, channel, PlayingSounds))
            return true;

        return false;
    }

    private bool StopSoundBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, 
        SoundChannelType channel, LinkedList<IAudioSource> audioSources, string? sound = null)
    {
        bool soundStopped = false;
        int priority = GetPriority(source, soundInfo, soundParams);
        LinkedListNode<IAudioSource>? node = audioSources.First;
        LinkedListNode<IAudioSource>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            var audioData = node.Value.AudioData;
            if (!ShouldStopSoundBySource(source, priority, channel, sound,
                audioData.SoundSource, audioData.SoundChannelType, audioData.SoundInfo.Name, audioData.Priority))
            {
                node = nextNode;
                continue;
            }

            node.Value.Stop();
            ArchiveCollection.DataCache.FreeAudioSource(node.Value);
            audioSources.Remove(node);
            soundStopped = true;

            node = nextNode;
        }

        return soundStopped;
    }

    private bool StopSoundBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams,
        SoundChannelType channel, LinkedList<WaitingSound> waitingSounds, string? sound = null)
    {
        bool soundStopped = false;
        int priority = GetPriority(source, soundInfo, soundParams);
        LinkedListNode<WaitingSound>? node = waitingSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            int otherPriority = GetPriority(node.Value.SoundSource, node.Value.SoundInfo, node.Value.SoundParams);
            if (!ShouldStopSoundBySource(source, priority, channel, sound,
                node.Value.SoundSource, node.Value.Channel, node.Value.SoundInfo.Name, otherPriority))
            {
                node = nextNode;
                continue;
            }

            waitingSounds.Remove(node);
            soundStopped = true;
            break;
        }

        return soundStopped;
    }

    private static bool ShouldStopSoundBySource(ISoundSource source, int priority, SoundChannelType channel, string? sound, 
        ISoundSource other, SoundChannelType otherChannel, string otherSound, int otherPriority)
    {
        if (!ReferenceEquals(source, other))
            return false;

        if (sound == null)
            return true;

        if (sound != otherSound || channel != otherChannel || otherPriority < priority)
            return false;

        return true;
    }

    public virtual IAudioSource? PlayStaticSound(string sound)
    {
        ISoundSource soundSource = DefaultSoundSource.Default;
        return CreateSound(soundSource, Vec3D.Zero, Vec3D.Zero, sound, SoundChannelType.Auto,
            new SoundParams(soundSource, attenuation: Attenuation.None), out _);
    }

    protected IAudioSource? CreateSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, string sound,
        SoundChannelType channel, SoundParams soundParams, out SoundInfo? soundInfo)
    {
        Precondition((int)channel < Entity.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
        soundInfo = GetSoundInfo(source, sound);
        if (soundInfo == null)
            return null;

        AttenuateIfNeeded(source, soundInfo, ref soundParams);

        int priority = GetPriority(source, soundInfo, soundParams);
        if (!CheckDistanceAndPriority(source, pos, velocity, soundInfo, channel, soundParams, priority))
            return null;

        if (HitSoundLimit(soundInfo) && !StopSoundsBySource(source, soundInfo, soundParams, channel))
            return null;

        AudioData audioData = new(source, soundInfo, channel, soundParams.Attenuation, priority, soundParams.Loop);
        IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, audioData);
        if (audioSource == null)
            return null;

        if (soundParams.Attenuation != Attenuation.None)
        {
            if (pos != null)
                audioSource.SetPosition(pos.Value.Float);
            if (velocity != null)
                audioSource.SetVelocity(velocity.Value.Float);
        }

        StopSoundsBySource(source, soundInfo, soundParams, channel);
        m_soundsToPlay.AddLast(audioSource);

        source?.SoundCreated(audioSource, channel);
        return audioSource;
    }

    private bool CheckDistanceAndPriority(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo, 
        SoundChannelType channel, in SoundParams soundParams, int priority)
    {
        double distance = GetDistance(source);
        bool soundTooFar = !CheckDistance(distance, soundParams.Attenuation);
        if (soundTooFar || SoundPriorityTooLow(source, channel, soundInfo, soundParams, distance, priority))
        {
            if (soundTooFar)
                StopSoundsBySource(source, soundInfo, soundParams, channel);

            if (soundParams.Loop)
                CreateWaitingLoopSound(source, pos, velocity, soundInfo, priority, channel, soundParams);

            return false;
        }

        return true;
    }

    private void CreateWaitingLoopSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo,
        int priority, SoundChannelType channel, in SoundParams soundParams)
    {
        var loopSound = new WaitingSound(source, pos, velocity, soundInfo, priority, channel, soundParams);
        m_waitingLoopSounds.AddLast(loopSound);
    }

    protected virtual void AttenuateIfNeeded(ISoundSource source, SoundInfo info, ref SoundParams soundParams)
    {
        // To be overridden if needed.
    }

    private bool SoundPriorityTooLow(ISoundSource source, SoundChannelType channel, SoundInfo soundInfo, in SoundParams soundParams, double distance, int priority)
    {
        // Check if this sound will remove a sound by it's source first, then check bumping by priority
        return IsMaxSoundCount && (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo, soundParams, channel) && !BumpSoundByPriority(priority, distance)));
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

            AddWaitingSoundFromBumpedSound(lowestPriorityNode.Value);
            ArchiveCollection.DataCache.FreeAudioSource(lowestPriorityNode.Value);

            audioSources.Remove(lowestPriorityNode);
            return true;
        }

        return false;
    }

    protected void AddWaitingSoundFromBumpedSound(IAudioSource audioSource)
    {
        if (!audioSource.AudioData.Loop)
            return;

        var soundParams = new SoundParams(audioSource.AudioData.SoundSource, true, audioSource.AudioData.Attenuation);
        CreateWaitingLoopSound(audioSource.AudioData.SoundSource, audioSource.GetPosition().Double, audioSource.GetVelocity().Double, audioSource.AudioData.SoundInfo,
            audioSource.AudioData.Priority, audioSource.AudioData.SoundChannelType, soundParams);
    }

    protected virtual int GetPriority(ISoundSource soundSource, SoundInfo soundInfo, SoundParams soundParams)
    {
        return 1;
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
                ArchiveCollection.DataCache.FreeAudioSource(node.Value);
                PlayingSounds.Remove(node.Value);
            }

            node = nextNode;
        }
    }
}

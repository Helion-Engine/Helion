using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World.Sound;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Sounds;

public class SoundManager : IDisposable
{
    public readonly IAudioSourceManager AudioManager;
    private readonly IRandom m_random = new TrueRandom();
    private readonly IAudioSystem m_audioSystem;
    private int m_maxConcurrentSounds = 32;

    protected ArchiveCollection ArchiveCollection;

    // The sounds that are currently playing
    protected readonly SoundList PlayingSounds = new();

    // The sounds that are generated in the same gametic that are waiting to be played as a group
    private readonly SoundList m_soundsToPlay = new();

    // Looping sounds that are saved but not currently playing.
    // It's either too far away to hear yet or was bumped by a higher priority sound.
    // These sounds are continually checked if they can be added in to play.
    private readonly WaitingSoundList m_waitingLoopSounds = new();

    // Intended for unit tests only
    public void SetMaxConcurrentSounds(int count) => m_maxConcurrentSounds = count;
    public LinkedList<IAudioSource> GetPlayingSounds() => PlayingSounds.ToLinkedList();
    public LinkedList<IAudioSource> GetSoundsToPlay() => m_soundsToPlay.ToLinkedList();
    public LinkedList<WaitingSound> GetWaitingSounds() => m_waitingLoopSounds;
    public bool PlaySound { get; set; } = true;

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

    private static IAudioSource? FindBySource(object source, SoundList audioSources)
    {
        var node = audioSources.Head;
        while (node != null)
        {
            if (ReferenceEquals(source, node.AudioData.SoundSource))
                return node;
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
        var node = PlayingSounds.Head;
        while (node != null)
        {
            node.Pause();
            node = node.Next;
        }
    }

    public void Resume()
    {
        var node = PlayingSounds.Head;
        while (node != null)
        {
            node.Play();
            node = node.Next;
        }
    }

    public void StopSoundBySource(ISoundSource source, SoundChannel channel, string sound)
    {
        if (!source.TryClearSound(sound, channel, out IAudioSource? clearedSound))
            return;

        if (clearedSound != null)
        {
            StopSound(clearedSound, m_soundsToPlay);
            StopSound(clearedSound, PlayingSounds);
        }

        StopSound(source, m_waitingLoopSounds);
    }

    protected void StopSound(IAudioSource audioSource, SoundList audioSources)
    {
        var node = audioSources.Head;
        while (node != null)
        {
            if (ReferenceEquals(audioSource, node))
            {
                node.Stop();
                audioSources.RemoveAndFree(node, ArchiveCollection.DataCache);
                return;
            }
            node = node.Next;
        }
    }

    protected void StopSound(ISoundSource soundSource, WaitingSoundList waitingSounds)
    {
        LinkedListNode<WaitingSound>? node = waitingSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (ReferenceEquals(soundSource, node.Value.SoundSource))
                waitingSounds.Free(node, ArchiveCollection.DataCache);

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

            if (IsMaxSoundCount && (HitSoundLimit(node.Value.SoundInfo) || !BumpSoundByPriority(node.Value.Priority, distance, node.Value.SoundParams.Attenuation)))
                return;

            var value = node.Value;
            var audio = CreateSound(value.SoundSource, value.Position, value.Velocity, value.SoundInfo.Name, value.SoundParams, out _);
            // If the sound was successfully created then remove from waiting loop sound list. Also check it wasn't already removed.
            if (audio != null && node.List == m_waitingLoopSounds)
                m_waitingLoopSounds.Free(node, ArchiveCollection.DataCache);

            node = nextNode;
        }
    }

    public void ClearSounds()
    {
        ClearSounds(m_soundsToPlay);
        ClearSounds(PlayingSounds);
        
        LinkedListNode<WaitingSound>? node = m_waitingLoopSounds.First;
        LinkedListNode<WaitingSound> freeNode;
        while (node != null)
        {
            freeNode = node;
            node = node.Next;
            m_waitingLoopSounds.Free(freeNode, ArchiveCollection.DataCache);
        }
    }

    private void ClearSounds(SoundList audioSources)
    {
        var node = audioSources.Head;
        while (node != null)
        {
            var nextNode = node.Next;
            node.Stop();
            audioSources.RemoveAndFree(node, ArchiveCollection.DataCache);
            node = nextNode;
        }
    }

    protected void PlaySounds()
    {
        if (m_soundsToPlay.Head == null)
            return;

        if (!PlaySound)
        {
            var clearNode = m_soundsToPlay.Head;
            while (clearNode != null)
            {
                clearNode.Stop();
                m_soundsToPlay.RemoveAndFree(clearNode, ArchiveCollection.DataCache);
                clearNode = clearNode.Next;
            }
            return;
        }

        AudioManager.PlayGroup(m_soundsToPlay);

        var node = m_soundsToPlay.Head;
        while (node != null)
        {
            var nextNode = node.Next;            
            m_soundsToPlay.Remove(node);
            PlayingSounds.Add(node);
            node = nextNode;
        }
    }

    private bool StopSoundsBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams)
    {
        // Always try to stop looping sounds that are waiting to be in range
        // This does not free up a sound if the limit has been hit
        StopSoundBySource(source, soundInfo, soundParams, m_waitingLoopSounds);

        if (StopSoundBySource(source, soundInfo, soundParams, m_soundsToPlay))
            return true;
        if (StopSoundBySource(source, soundInfo, soundParams, PlayingSounds))
            return true;

        return false;
    }

    private bool StopSoundBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, 
        SoundList audioSources, string? sound = null)
    {
        bool soundStopped = false;
        int priority = GetPriority(source, soundInfo, soundParams);
        var node = audioSources.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            var audioData = node.AudioData;
            if (!ShouldStopSoundBySource(source, priority, soundParams.Channel, sound,
                audioData.SoundSource, audioData.SoundChannelType, audioData.SoundInfo.Name, audioData.Priority))
            {
                node = nextNode;
                continue;
            }

            node.Stop();
            audioSources.RemoveAndFree(node, ArchiveCollection.DataCache);
            soundStopped = true;
            break;
        }

        return soundStopped;
    }

    private bool StopSoundBySource(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams,
        WaitingSoundList waitingSounds, string? sound = null)
    {
        bool soundStopped = false;
        int priority = GetPriority(source, soundInfo, soundParams);
        LinkedListNode<WaitingSound>? node = waitingSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            int otherPriority = GetPriority(node.Value.SoundSource, node.Value.SoundInfo, node.Value.SoundParams);
            if (!ShouldStopSoundBySource(source, priority, soundParams.Channel, sound,
                node.Value.SoundSource, node.Value.SoundParams.Channel, node.Value.SoundInfo.Name, otherPriority))
            {
                node = nextNode;
                continue;
            }

            waitingSounds.Remove(node);
            ArchiveCollection.DataCache.FreeWaitingSoundNode(node);
            soundStopped = true;
            break;
        }

        return soundStopped;
    }

    private static bool ShouldStopSoundBySource(ISoundSource source, int priority, SoundChannel channel, string? sound, 
        ISoundSource other, SoundChannel otherChannel, string otherSound, int otherPriority)
    {
        if (!ReferenceEquals(source, other))
            return false;

        if (channel != otherChannel || otherPriority < priority)
            return false;

        if (sound == null)
            return true;

        if (sound != otherSound)
            return false;

        return true;
    }

    public virtual IAudioSource? PlayStaticSound(string sound)
    {
        ISoundSource soundSource = DefaultSoundSource.Default;
        return CreateSound(soundSource, Vec3D.Zero, Vec3D.Zero, sound,
            new SoundParams(soundSource, attenuation: Attenuation.None), out _);
    }

    protected IAudioSource? CreateSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, string sound,
        SoundParams soundParams, out SoundInfo? soundInfo)
    {
        Precondition((int)soundParams.Channel < Constants.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
        soundInfo = GetSoundInfo(source, sound);
        if (soundInfo == null)
            return null;

        AttenuateIfNeeded(source, soundInfo, ref soundParams);

        int priority = GetPriority(source, soundInfo, soundParams);
        if (!CheckDistanceAndPriority(source, pos, velocity, soundInfo, soundParams, priority))
            return null;

        if (HitSoundLimit(soundInfo) && !StopSoundsBySource(source, soundInfo, soundParams))
            return null;

        AudioData audioData = new(source, soundInfo, soundParams.Channel, soundParams.Attenuation, priority, soundParams.Loop);
        IAudioSource? audioSource = AudioManager.Create(soundInfo.EntryName, audioData);
        if (audioSource == null)
            return null;

        if (soundParams.Attenuation != Attenuation.None)
        {
            if (pos != null)
                audioSource.SetPosition((float)pos.Value.X, (float)pos.Value.Y, (float)pos.Value.Z);
            if (velocity != null)
                audioSource.SetVelocity((float)velocity.Value.X, (float)velocity.Value.Y, (float)velocity.Value.Z);
        }

        StopSoundsBySource(source, soundInfo, soundParams);
        m_soundsToPlay.Add(audioSource);

        source?.SoundCreated(soundInfo, audioSource, soundParams.Channel);
        return audioSource;
    }

    private bool CheckDistanceAndPriority(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo, 
        in SoundParams soundParams, int priority)
    {
        double distance = GetDistance(source);
        bool soundTooFar = !CheckDistance(distance, soundParams.Attenuation);
        if (soundTooFar || SoundPriorityTooLow(source, soundInfo, soundParams, distance, priority))
        {
            if (soundTooFar)
                StopSoundsBySource(source, soundInfo, soundParams);

            if (soundParams.Loop)
                CreateWaitingLoopSound(source, pos, velocity, soundInfo, priority, soundParams);

            return false;
        }

        return true;
    }

    private void CreateWaitingLoopSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo,
        int priority, in SoundParams soundParams)
    {
        var loopSound = new WaitingSound(source, pos, velocity, soundInfo, priority, soundParams);
        m_waitingLoopSounds.AddLast(ArchiveCollection.DataCache.GetWaitingSoundNode(loopSound));
        source.SoundCreated(soundInfo, null, soundParams.Channel);
    }

    protected virtual void AttenuateIfNeeded(ISoundSource source, SoundInfo info, ref SoundParams soundParams)
    {
        // To be overridden if needed.
    }

    private bool SoundPriorityTooLow(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, double distance, int priority)
    {
        if (!IsMaxSoundCount)
            return false;

        // Check if this sound will remove a sound by it's source first, then check bumping by priority
        return (HitSoundLimit(soundInfo) || (!StopSoundsBySource(source, soundInfo, soundParams) && 
            !BumpSoundByPriority(priority, distance, soundParams.Attenuation)));
    }

    private bool HitSoundLimit(SoundInfo soundInfo)
    {
        return soundInfo.Limit > 0 && GetSoundCount(soundInfo) >= soundInfo.Limit;
    }

    private bool IsMaxSoundCount => m_soundsToPlay.Count + PlayingSounds.Count >= m_maxConcurrentSounds;

    private bool BumpSoundByPriority(int priority, double distance, Attenuation attenuation)
    {
        if (BumpSoundByPriority(priority, distance, attenuation, m_soundsToPlay))
            return true;
        if (BumpSoundByPriority(priority, distance, attenuation, PlayingSounds))
            return true;

        return false;
    }

    private bool BumpSoundByPriority(int priority, double distance, Attenuation attenuation, SoundList audioSources)
    {
        int lowestPriority = 0;
        double farthestDistance = 0;
        IAudioSource? lowestPriorityNode = null;
        IAudioSource? node = audioSources.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (node.AudioData.Attenuation != Attenuation.None && node.AudioData.Priority > lowestPriority)
            {
                double checkDistance = GetDistance(node.AudioData.SoundSource);
                if (checkDistance > farthestDistance)
                {
                    lowestPriorityNode = node;
                    lowestPriority = node.AudioData.Priority;
                    farthestDistance = checkDistance;
                }
            }

            node = nextNode;
        }

        if (lowestPriorityNode != null && priority <= lowestPriority && (distance < farthestDistance || attenuation == Attenuation.None))
        {
            lowestPriorityNode.Stop();
            audioSources.RemoveAndFree(lowestPriorityNode, ArchiveCollection.DataCache);
            AddWaitingSoundFromBumpedSound(lowestPriorityNode);
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
            audioSource.AudioData.Priority, soundParams);
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
        var node = PlayingSounds.Head;

        while (node != null)
        {
            if (soundInfo.Equals(node.AudioData.SoundInfo))
                count++;

            node = node.Next;
        }

        return count;
    }

    public virtual void Update()
    {
        AudioManager.Tick();

        // Note: We do not set the position here since everything should be
        // attenuated globally.
        UpdateWaitingLoopSounds();
        PlaySounds();

        if (PlayingSounds.Count == 0)
            return;

        IAudioSource? node = PlayingSounds.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (node.IsFinished())
                PlayingSounds.RemoveAndFree(node, ArchiveCollection.DataCache);
            node = nextNode;
        }
    }
}

using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Sound;
using System;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Sounds;

public class SoundManager : IDisposable
{
    public event EventHandler<SoundCreatedEventArgs>? SoundCreated;

    public readonly IAudioSourceManager AudioManager;
    private readonly IRandom m_random = new TrueRandom();
    private readonly IAudioSystem m_audioSystem;
    protected int m_maxConcurrentSounds = 32;
    protected int m_sameSoundLimit;
    protected int m_sameSoundWindow;

    protected ArchiveCollection ArchiveCollection;

    // The sounds that are currently playing
    protected readonly SoundList PlayingSounds = new();

    protected bool m_setVelocity;

    // The sounds that are generated in the same gametic that are waiting to be played as a group
    private readonly SoundList m_soundsToPlay = new();

    // Looping sounds that are saved but not currently playing.
    // It's either too far away to hear yet or was bumped by a higher priority sound.
    // These sounds are continually checked if they can be added in to play.
    private readonly WaitingSoundList m_waitingLoopSounds = new();

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

    public IEnumerable<EventHandler<SoundCreatedEventArgs>> GetSoundCreatedEventListeners()
    {
        foreach (Delegate del in SoundCreated?.GetInvocationList() ?? [])
        {
            yield return (EventHandler<SoundCreatedEventArgs>)del;
        }
    }

    ~SoundManager()
    {
        ReleaseUnmanagedResources();
        FailedToDispose(this);
    }

    protected virtual void HandleDispose()
    {

    }

    public void Dispose()
    {
        ClearSounds();
        ReleaseUnmanagedResources();
        HandleDispose();

        foreach (Delegate del in SoundCreated?.GetInvocationList() ?? [])
        {
            SoundCreated -= (EventHandler<SoundCreatedEventArgs>)del;
        }

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

    protected virtual double GetDistanceSquared(ISoundSource soundSource) => 0;

    protected virtual IRandom GetRandom() => m_random;

    protected virtual int GetGameTick() => 0;

    protected void UpdateWaitingLoopSounds()
    {
        var gametick = GetGameTick();
        LinkedListNode<WaitingSound>? node = m_waitingLoopSounds.First;
        LinkedListNode<WaitingSound>? nextNode;
        LinkedListNode<WaitingSound>? nextNextNode;
        while (node != null)
        {
            if (node.List == null)
                break;

            nextNode = node.Next;
            nextNextNode = nextNode?.Next;
            var distanceSquared = GetDistanceSquared(node.Value.SoundSource);

            if (!CheckDistance(distanceSquared, node.Value.SoundParams.Attenuation))
            {
                node = nextNode;
                continue;
            }

            if ((IsMaxSoundCount || HitSoundLimit(node.Value.SoundInfo, gametick)) && !BumpSoundByPriority(node.Value.Priority, distanceSquared, node.Value.SoundParams.Attenuation))
                return;

            var value = node.Value;
            var elaspedSeconds = (GetGameTick() - value.GameTick) / 35f;
            var audio = CreateSound(value.SoundSource, value.Position, value.Velocity, value.OffsetSeconds + elaspedSeconds, value.SoundInfo.Name, value.SoundParams, out _);
            // If the sound was successfully created then remove from waiting loop sound list. Also check it wasn't already removed.
            if (audio != null && node.List == m_waitingLoopSounds)
                m_waitingLoopSounds.Free(node, ArchiveCollection.DataCache);


            // CreateSound can remove the nextNode in the chain. Need to check if it was removed and use the next one ahead.
            node = nextNode;
            if (node?.List == null)
                node = nextNextNode;
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

    private bool StopSounds(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, double distanceSquared, StopSoundOption option)
    {
        var sound = option == StopSoundOption.BySource ? null : soundInfo.Name;
        // Always try to stop looping sounds that are waiting to be in range
        // This does not free up a sound if the limit has been hit
        StopSound(source, soundInfo, soundParams, option, m_waitingLoopSounds, sound: sound);

        if (StopSound(source, soundInfo, soundParams, option, distanceSquared, m_soundsToPlay, sound: sound))
            return true;
        if (StopSound(source, soundInfo, soundParams, option, distanceSquared, PlayingSounds, sound: sound))
            return true;

        return false;
    }

    private bool StopSound(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, StopSoundOption option, double sourceDistanceSquared,
        SoundList audioSources, string? sound = null)
    {
        bool soundStopped = false;
        var priority = GetPriority(source, soundInfo, soundParams);
        var node = audioSources.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            var audioData = node.AudioData;
            if (!ShouldStopSound(source, priority, soundParams.Channel, sound,
                audioData.SoundSource, audioData.SoundChannelType, audioData.SoundInfo.Name, audioData.Priority, option))
            {
                node = nextNode;
                continue;
            }

            if (option == StopSoundOption.BySound)
            {
                if (sourceDistanceSquared > GetDistanceSquared(node.AudioData.SoundSource))
                {
                    node = nextNode;
                    continue;
                }
            }

            node.Stop();
            audioSources.RemoveAndFree(node, ArchiveCollection.DataCache);
            soundStopped = true;
            break;
        }

        return soundStopped;
    }

    private bool StopSound(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, StopSoundOption option,
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
            if (!ShouldStopSound(source, priority, soundParams.Channel, sound,
                node.Value.SoundSource, node.Value.SoundParams.Channel, node.Value.SoundInfo.Name, otherPriority, option))
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

    private static bool ShouldStopSound(ISoundSource source, int priority, SoundChannel channel, string? sound, 
        ISoundSource other, SoundChannel otherChannel, string otherSound, int otherPriority, StopSoundOption option)
    {
        if (option == StopSoundOption.BySource && !ReferenceEquals(source, other))
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
        return CreateSound(soundSource, Vec3D.Zero, Vec3D.Zero, 0, sound,
            new SoundParams(soundSource, attenuation: Attenuation.None), out _);
    }

    protected IAudioSource? CreateSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, float offset, string sound,
        SoundParams soundParams, out SoundInfo? soundInfo)
    {
        Precondition((int)soundParams.Channel < Constants.MaxSoundChannels, "ZDoom extra channel flags unsupported currently");
        soundInfo = GetSoundInfo(source, sound);
        if (soundInfo == null)
            return null;

        SetSoundParams(source, soundInfo, ref soundParams);

        var gametick = GetGameTick();
        var priority = GetPriority(source, soundInfo, soundParams);
        var distanceSquared = GetDistanceSquared(source);
        if (!CheckDistanceAndPriority(source, pos, velocity, soundInfo, soundParams, priority, 0, gametick, distanceSquared))
            return null;

        var hitSoundLimit =  IsMaxSoundCount || HitSoundLimit(soundInfo, gametick);
        if (hitSoundLimit && !StopSounds(source, soundInfo, soundParams, distanceSquared, StopSoundOption.BySource))
        {
            if (!StopSounds(source, soundInfo, soundParams, distanceSquared, StopSoundOption.BySound))
                return null;
        }

        var audioData = new AudioData(source, soundInfo, soundParams.Channel, soundParams.Attenuation, priority, soundParams.Loop, soundParams.Relative, soundParams.Volume, soundParams.AttenuationFactor, offset, gametick);
        var audioSource = AudioManager.Create(soundInfo.EntryName, audioData);
        if (audioSource == null)
            return null;

        if (soundParams.Attenuation != Attenuation.None)
        {
            if (pos != null)
                audioSource.SetPosition((float)pos.Value.X, (float)pos.Value.Y, (float)pos.Value.Z);
            if (m_setVelocity && velocity != null)
                audioSource.SetVelocity((float)velocity.Value.X, (float)velocity.Value.Y, (float)velocity.Value.Z);
        }

        StopSounds(source, soundInfo, soundParams, distanceSquared, StopSoundOption.BySource);
        m_soundsToPlay.Add(audioSource);

        source?.SoundCreated(soundInfo, audioSource, soundParams.Channel);
        SoundCreated?.Invoke(this, new SoundCreatedEventArgs() { SoundInfo = soundInfo, SoundParams = soundParams, SoundSource = source});
        return audioSource;
    }

    private bool CheckDistanceAndPriority(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo, 
        in SoundParams soundParams, int priority, float offset, int gametick, double distanceSquared)
    {
        var soundTooFar = !CheckDistance(distanceSquared, soundParams.Attenuation);
        if (soundTooFar || SoundPriorityTooLow(source, soundInfo, soundParams, distanceSquared, priority, gametick))
        {
            if (soundTooFar)
                StopSounds(source, soundInfo, soundParams, distanceSquared, StopSoundOption.BySource);

            if (soundParams.Loop)
                CreateWaitingLoopSound(source, pos, velocity, soundInfo, priority, offset, 0, soundParams);

            return false;
        }

        return true;
    }

    private void CreateWaitingLoopSound(ISoundSource source, in Vec3D? pos, in Vec3D? velocity, SoundInfo soundInfo,
        int priority, float offset, int gameTick, in SoundParams soundParams)
    {
        var loopSound = new WaitingSound(source, pos, velocity, soundInfo, priority, offset, gameTick, soundParams);
        m_waitingLoopSounds.AddLast(ArchiveCollection.DataCache.GetWaitingSoundNode(loopSound));
        source.SoundCreated(soundInfo, null, soundParams.Channel);
    }

    protected virtual void SetSoundParams(ISoundSource source, SoundInfo info, ref SoundParams soundParams)
    {
        // To be overridden if needed.
    }

    private bool SoundPriorityTooLow(ISoundSource source, SoundInfo soundInfo, in SoundParams soundParams, double distanceSquared, int priority, int gametick)
    {
        if (!IsMaxSoundCount)
            return false;

        // Check if this sound will remove a sound by it's source first, then check bumping by priority
        return HitSoundLimit(soundInfo, gametick) || (!StopSounds(source, soundInfo, soundParams, distanceSquared, StopSoundOption.BySource) && 
            !BumpSoundByPriority(priority, distanceSquared, soundParams.Attenuation));
    }

    private bool HitSoundLimit(SoundInfo soundInfo, int gametick)
    {
        if (soundInfo.Limit <= 0 && m_sameSoundLimit <= 0)
            return false;

        var soundCount = GetSoundCount(soundInfo, gametick);
        if (soundInfo.Limit > 0 && soundCount >= soundInfo.Limit)
            return true;

        if (m_sameSoundLimit > 0 && soundCount >= m_sameSoundLimit)
            return true;

        return false;
    }

    private bool IsMaxSoundCount => m_soundsToPlay.Count + PlayingSounds.Count >= m_maxConcurrentSounds;

    private bool BumpSoundByPriority(int priority, double distanceSquared, Attenuation attenuation)
    {
        if (BumpSoundByPriority(priority, distanceSquared, attenuation, m_soundsToPlay))
            return true;
        if (BumpSoundByPriority(priority, distanceSquared, attenuation, PlayingSounds))
            return true;

        return false;
    }

    private bool BumpSoundByPriority(int priority, double distanceSquared, Attenuation attenuation, SoundList audioSources)
    {
        int lowestPriority = 0;
        double farthestDistanceSquared = 0;
        IAudioSource? lowestPriorityNode = null;
        IAudioSource? node = audioSources.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (node.AudioData.Attenuation != Attenuation.None && node.AudioData.Priority > lowestPriority)
            {
                var checkDistanceSquared = GetDistanceSquared(node.AudioData.SoundSource);
                if (checkDistanceSquared > farthestDistanceSquared)
                {
                    lowestPriorityNode = node;
                    lowestPriority = node.AudioData.Priority;
                    farthestDistanceSquared = checkDistanceSquared;
                }
            }

            node = nextNode;
        }

        if (lowestPriorityNode != null && priority <= lowestPriority && (distanceSquared < farthestDistanceSquared || attenuation == Attenuation.None))
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
            audioSource.AudioData.Priority, audioSource.GetOffsetSeconds(), GetGameTick(), soundParams);
    }

    protected virtual int GetPriority(ISoundSource soundSource, SoundInfo soundInfo, SoundParams soundParams)
    {
        return 1;
    }

    protected static bool CheckDistance(double distanceSquared, Attenuation attenuation)
    {
        return attenuation == Attenuation.None || distanceSquared <= Constants.MaxSoundDistance * Constants.MaxSoundDistance;
    }

    protected virtual SoundInfo? GetSoundInfo(ISoundSource? source, string sound)
    {
        return ArchiveCollection.Definitions.SoundInfo.Lookup(sound, GetRandom());
    }

    public int GetSoundCount(SoundInfo? soundInfo, int gametick)
    {
        if (soundInfo == null)
            return 0;

        int count = 0;
        var node = PlayingSounds.Head;

        while (node != null)
        {
            if (soundInfo == node.AudioData.SoundInfo && CheckSoundWindow(node, gametick))
                count++;

            node = node.Next;
        }

        node = m_soundsToPlay.Head;
        while (node != null)
        {
            if (soundInfo == node.AudioData.SoundInfo && CheckSoundWindow(node, gametick))
                count++;

            node = node.Next;
        }

        return count;
    }

    private bool CheckSoundWindow(IAudioSource audio, int gametick)
    {
        if (m_sameSoundWindow == 0)
            return true;

        if (gametick - audio.AudioData.GameTick <= m_sameSoundWindow)
            return true;

        return false;
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

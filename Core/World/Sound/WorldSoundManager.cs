using System;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Players;

namespace Helion.World.Sound;

public class WorldSoundManager : SoundManager, ITickable
{
    private IWorld m_world;

    public WorldSoundManager(IWorld world, IAudioSystem audioSystem) : base(audioSystem, world.ArchiveCollection)
    {
        m_world = world;
        InitSoundConfig(world);
        RegisterEvents(world);
    }

    private void InitSoundConfig(IWorld world)
    {
        m_maxConcurrentSounds = world.Config.Audio.MaxSounds;
        m_sameSoundLimit = world.Config.Audio.SameSoundLimit;
        m_sameSoundWindow = world.Config.Audio.SameSoundWindow;
        m_setVelocity = world.Config.Audio.Velocity;
    }

    public void UpdateTo(IWorld world)
    {
        m_world = world;
        InitSoundConfig(world);
        UnregisterEvents(m_world);
        RegisterEvents(world);
        ArchiveCollection = world.ArchiveCollection;
        ClearSounds();
        AudioManager.Clear();
    }

    public void UnregisterEvents() => UnregisterEvents(m_world);

    private void RegisterEvents(IWorld world)
    {
        world.Config.Audio.MaxSounds.OnChanged += MaxSounds_OnChanged;
        world.Config.Audio.SameSoundLimit.OnChanged += SameSoundLimit_OnChanged;
        world.Config.Audio.SameSoundWindow.OnChanged += SameSoundWindow_OnChanged;
        world.Config.Audio.Velocity.OnChanged += Velocity_OnChanged;
    }

    private void UnregisterEvents(IWorld world)
    {
        world.Config.Audio.MaxSounds.OnChanged -= MaxSounds_OnChanged;
        world.Config.Audio.SameSoundLimit.OnChanged -= SameSoundLimit_OnChanged;
        world.Config.Audio.SameSoundWindow.OnChanged -= SameSoundWindow_OnChanged;
        world.Config.Audio.Velocity.OnChanged -= Velocity_OnChanged;
    }

    private void MaxSounds_OnChanged(object? sender, int max) =>  m_maxConcurrentSounds = max;
    private void SameSoundLimit_OnChanged(object? sender, int limit) => m_sameSoundLimit = limit;
    private void SameSoundWindow_OnChanged(object? sender, int window) => m_sameSoundWindow = window;
    private void Velocity_OnChanged(object? sender, bool set) => m_setVelocity = set;

    protected override IRandom GetRandom() => m_world.Random;

    protected override int GetGameTick() => m_world.Gametick;

    protected override double GetDistanceSquared(ISoundSource soundSource)
    {
        return soundSource.GetDistanceSquaredFrom(m_world.GetListener().Entity);
    }

    protected override void HandleDispose()
    {
        UnregisterEvents(m_world);
    }

    public override IAudioSource? PlayStaticSound(string sound)
    {
        ISoundSource soundSource = DefaultSoundSource.Default;
        return m_world.SoundManager.CreateSoundOn(soundSource, sound,
            new SoundParams(soundSource, attenuation: Attenuation.None));
    }

    public IAudioSource? CreateSoundOn(ISoundSource soundSource, string sound, SoundParams soundParams)
    {
        if (!soundSource.CanMakeSound())
            return null;

        IAudioSource? source = CreateSound(soundSource, soundSource.GetSoundPosition(m_world.GetListener().Entity), soundSource.GetSoundVelocity(), 0,
            sound, soundParams, out SoundInfo? soundInfo);
        if (source == null)
            return source;

        if (soundInfo != null)
            SetPitchModifiers(soundSource, source, soundInfo);

        if (m_world.Config.Audio.Pitch != 1)
            source.SetPitch(source.GetPitch() * (float)m_world.Config.Audio.Pitch);

        return source;
    }

    private void SetPitchModifiers(ISoundSource soundSource, IAudioSource source, SoundInfo soundInfo)
    {
        bool pitchSet = soundInfo.PitchSet > 0;
        if (pitchSet)
        {
            source.SetPitch(soundInfo.PitchSet);
            return;
        }

        if (ShouldRandomizePitch(soundSource))
        {
            int pitchShift = 1 << soundInfo.PitchShift;
            if (pitchShift > 1)
                SetPitchShift(source, pitchShift);
        }
    }

    private void SetPitchShift(IAudioSource source, int pitchShift)
    {
        // Doom's default pitch shift range is 4.
        // Default add value is 16 and clamp value is 31.
        // Saw is modified to 3 (with 8 add and 15 clamp).
        const float NormalPitch = 128f;
        int clamp = pitchShift * 2 - 1;
        int rand = (int)Math.Clamp((m_world.SecondaryRandom.NextByte() & clamp) * m_world.Config.Audio.RandomPitchScale, 1, 255);
        int add = (int)Math.Clamp(pitchShift * m_world.Config.Audio.RandomPitchScale, 1, 255);
        float pitch = Math.Clamp(NormalPitch + add - rand, 0, 255);
        source.SetPitch(pitch / NormalPitch);
    }

    private bool ShouldRandomizePitch(ISoundSource soundSource)
    {
        if (m_world.Config.Audio.RandomizePitch == RandomPitch.None)
            return false;

        if (m_world.Config.Audio.RandomizePitch == RandomPitch.All)
            return true;

        return soundSource is Entity entity && (entity.Flags.CountKill || entity.Flags.IsMonster);
    }

    protected override int GetPriority(ISoundSource soundSource, SoundInfo soundInfo, SoundParams soundParams)
    {
        // Sounds from the listener are top priority.
        // Sounds that do not attenuate are next, then prioritize sounds by the type the entity is producing.
        if (ReferenceEquals(soundSource, m_world.GetListener().Entity))
            return 0;

        if (soundParams.Attenuation == Attenuation.None)
            return 1;

        // Checking there is no owner, otherwise rockets set the see type and get bumped out by moving floors
        if (soundSource is Entity entity && !entity.IsPlayer && entity.Owner() == null)
        {
            switch (soundParams.SoundType)
            {
                case SoundType.Pain:
                    return 3;
                case SoundType.See:
                    return 4;
                case SoundType.Active:
                    return 5;
                default:
                    break;
            }
        }

        return 2;
    }

    protected override SoundInfo? GetSoundInfo(ISoundSource? source, string sound)
    {
        if (source is Player player)
        {
            string playerSound = SoundInfoDefinition.GetPlayerSound(player.Info.GetGender(), sound);
            SoundInfo? soundInfo = ArchiveCollection.Definitions.SoundInfo.Lookup(playerSound, m_world.Random);
            if (soundInfo != null && ArchiveCollection.Entries.FindByName(soundInfo.EntryName) != null)
                return soundInfo;

            // Sound likely does not exist for user selected gender - fallback to default
            playerSound = SoundInfoDefinition.GetPlayerSound("male", sound);
            soundInfo = ArchiveCollection.Definitions.SoundInfo.Lookup(playerSound, m_world.Random);
            if (soundInfo != null)
                return soundInfo;

        }

        return base.GetSoundInfo(source, sound);
    }

    protected override void SetSoundParams(ISoundSource source, SoundInfo info, ref SoundParams soundParams)
    {
        // If sound is generated by the lisenter then set relative to the listeners position
        if (ReferenceEquals(source, m_world.GetListener().Entity))
            soundParams.Relative = true;
    }

    public void MakeSoundsNotRelativeTo(ISoundSource source)
    {
        var playingSound = PlayingSounds.Head;
        while (playingSound != null)
        {
            if (playingSound.AudioData.SoundSource == source)
                playingSound.SetRelative(false);
            playingSound = playingSound.Next;
        }
    }

    public override void Update()
    {
        Tick();
    }

    public void Tick()
    {
        if (m_world.IsDisposed)
            return;

        var listener = m_world.GetListener();
        AudioManager.SetListener(listener.Position, listener.Angle, listener.Pitch);
        UpdateWaitingSounds();
        PlaySounds();
        AudioManager.Tick();

        if (PlayingSounds.Count == 0)
            return;

        var node = PlayingSounds.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (node.IsFinished())
            {
                node.AudioData.SoundSource.TryClearSound(node.AudioData.SoundInfo.Name, SoundChannel.Default, out _);
                PlayingSounds.RemoveAndFree(node, m_world.DataCache);
                node = nextNode;
                continue;
            }

            var distanceSquared = node.AudioData.SoundSource.GetDistanceSquaredFrom(listener.Entity);
            if (!CheckDistance(distanceSquared, node.AudioData.Attenuation))
            {
                AddWaitingSoundFromBumpedSound(node);
                node.Stop();
                PlayingSounds.RemoveAndFree(node, m_world.DataCache);
            }
            else
            {
                node.Update(new((float)Math.Sqrt(distanceSquared)));
                var position = node.AudioData.SoundSource.GetSoundPosition(listener.Entity);
                if (position != null)
                    node.SetPosition((float)position.Value.X, (float)position.Value.Y, (float)position.Value.Z);
            }
            node = nextNode;
        }
    }
}

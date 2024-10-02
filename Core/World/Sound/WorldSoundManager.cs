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

public class WorldSoundManager(IWorld world, IAudioSystem audioSystem) : SoundManager(audioSystem, world.ArchiveCollection), ITickable
{
    private IWorld m_world = world;

    public void UpdateTo(IWorld world)
    {
        m_world = world;
        ArchiveCollection = world.ArchiveCollection;
        ClearSounds();
        AudioManager.Clear();
    }

    protected override IRandom GetRandom() => m_world.Random;

    protected override double GetDistance(ISoundSource soundSource)
    {
        return soundSource.GetDistanceFrom(m_world.GetListener().Entity);
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

        IAudioSource? source = CreateSound(soundSource, soundSource.GetSoundPosition(m_world.GetListener().Entity), soundSource.GetSoundVelocity(),
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
        if (soundSource is Entity entity && !entity.IsPlayer && entity.Owner.Entity == null)
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
            if (soundInfo != null && ArchiveCollection.Entries.FindByName(playerSound) != null)
                return soundInfo;

            // Sound likely does not exist for user selected gender - fallback to default
            playerSound = SoundInfoDefinition.GetPlayerSound("male", sound);
            soundInfo = ArchiveCollection.Definitions.SoundInfo.Lookup(playerSound, m_world.Random);
            if (soundInfo != null)
                return soundInfo;

        }

        return base.GetSoundInfo(source, sound);
    }

    protected override void AttenuateIfNeeded(ISoundSource source, SoundInfo info, ref SoundParams soundParams)
    {
        // Don't attenuate sounds generated by the listener, otherwise movement can cause the sound to be off
        if (ReferenceEquals(source, m_world.GetListener().Entity))
            soundParams.Attenuation = Attenuation.None;
    }

    public override void Update()
    {
        Tick();
    }

    public void Tick()
    {
        if (m_world.IsDisposed)
            return;

        m_setVelocity = ArchiveCollection.Config.Audio.Velocity;
        var listener = m_world.GetListener();
        AudioManager.SetListener(listener.Position, listener.Angle, listener.Pitch);
        UpdateWaitingLoopSounds();
        PlaySounds();
        AudioManager.Tick();

        if (PlayingSounds.Count == 0)
            return;

        IAudioSource? node = PlayingSounds.Head;
        IAudioSource? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            if (node.IsFinished())
            {
                PlayingSounds.RemoveAndFree(node, m_world.DataCache);
                node = nextNode;
                continue;
            }

            double distance = node.AudioData.SoundSource.GetDistanceFrom(listener.Entity);
            if (!CheckDistance(distance, node.AudioData.Attenuation))
            {
                node.Stop();
                PlayingSounds.RemoveAndFree(node, m_world.DataCache);
                AddWaitingSoundFromBumpedSound(node);
            }
            else
            {
                var position = node.AudioData.SoundSource.GetSoundPosition(listener.Entity);
                if (position != null)
                    node.SetPosition((float)position.Value.X, (float)position.Value.Y, (float)position.Value.Z);
            }
            node = nextNode;
        }
    }
}

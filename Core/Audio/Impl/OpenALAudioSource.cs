using System;
using Helion.Audio.Impl.Components;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl;

public class OpenALAudioSource : IAudioSource
{
    private const float DefaultRolloff = 2.5f;
    private const float DefaultReference = 296.0f;
    private const float DefaultMaxDistance = (float)Constants.MaxSoundDistance;
    private const float DefaultRadius = 32.0f;
    private const ALSourcef SourceRadius = (ALSourcef)0x1031;
    private const ALSourcei SourceDistanceModel = (ALSourcei)53248;

    public event EventHandler? Completed;

    public Vec3F Velocity { get; set; }

    public AudioData AudioData { get; set; }
    public OpenALAudioSourceManager Owner { get; private set; }
    private int m_sourceId;
    private bool m_disposed;

    public int ID => m_sourceId;

    public OpenALAudioSource(OpenALAudioSourceManager owner, OpenALBuffer buffer, AudioData audioData, SoundParams soundParams)
    {
        Set(owner, buffer, audioData, soundParams);
        Owner = owner;
        AudioData = audioData;
    }

    public void Set(OpenALAudioSourceManager owner, OpenALBuffer buffer, AudioData audioData, SoundParams soundParams)
    {
        Owner = owner;
        AudioData = audioData;

        float rolloffFactor = DefaultRolloff;
        float referenceDistance = DefaultReference;
        float maxDistance = DefaultMaxDistance;
        float radius = DefaultRadius;

        if (soundParams.SoundSource is Entity entity)
            radius = (float)entity.Radius + 16.0f;
        else if (soundParams.SoundSource is Sector)
            radius = 128.0f;

        switch (soundParams.Attenuation)
        {
            case Attenuation.None:
                // Max out the distance to prevent directional sound from taking effect
                radius = 65536.0f;
                referenceDistance = 0.0f;
                maxDistance = 0.0f;
                rolloffFactor = 0.0f;
                break;
            case Attenuation.Rapid:
                rolloffFactor = DefaultRolloff * 2.0f;
                break;
            case Attenuation.Default:
            default:
                break;
        }

        OpenALDebug.Start("Creating new source");
        m_sourceId = AL.GenSource();
        AL.Source(m_sourceId, ALSourcef.MinGain, 0.0f);
        AL.Source(m_sourceId, ALSourcef.RolloffFactor, rolloffFactor);
        AL.Source(m_sourceId, ALSourcef.ReferenceDistance, referenceDistance);
        AL.Source(m_sourceId, SourceRadius, radius);
        AL.Source(m_sourceId, ALSourcef.MaxDistance, maxDistance);
        AL.Source(m_sourceId, ALSourcef.Pitch, 1.0f);
        AL.Source(m_sourceId, ALSourceb.Looping, soundParams.Loop);
        AL.Source(m_sourceId, ALSourcei.Buffer, buffer.BufferId);
        OpenALDebug.End("Creating new source");


        DataCache.Instance.FreeSoundParams(soundParams);
    }

    public void SetPosition(Vec3F pos)
    {
        OpenALDebug.Start("Setting sound position");
        AL.Source(m_sourceId, ALSource3f.Position, pos.X, pos.Y, pos.Z);
        OpenALDebug.End("Setting sound position");
    }

    public float GetPitch()
    {
        OpenALDebug.Start("Getting sound position");
        AL.GetSource(m_sourceId, ALSourcef.Pitch, out float pitch);
        OpenALDebug.End("Getting sound position");
        return pitch;
    }

    public void SetPitch(float pitch)
    {
        OpenALDebug.Start("Setting sound pitch");
        AL.Source(m_sourceId, ALSourcef.Pitch, pitch);
        OpenALDebug.End("Setting sound pitch");
    }

    public Vec3F GetPosition()
    {
        OpenALDebug.Start("Getting sound position");
        AL.GetSource(m_sourceId, ALSource3f.Position, out Vector3 pos);
        OpenALDebug.End("Getting sound position");

        return new Vec3F(pos.X, pos.Y, pos.Z);
    }

    public void SetVelocity(Vec3F velocity)
    {
        OpenALDebug.Start("Setting sound velocity");
        AL.Source(m_sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
        OpenALDebug.End("Setting sound velocity");
    }

    ~OpenALAudioSource()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public override int GetHashCode() => m_sourceId;

    public override bool Equals(object? obj)
    {
        if (obj is OpenALAudioSource audioSource)
            return audioSource.m_sourceId == m_sourceId;

        return false;
    }

    public void Play()
    {
        if (!m_disposed)
        {
            OpenALDebug.Start("Playing sound");
            AL.SourcePlay(m_sourceId);
            OpenALDebug.End("Playing sound");
        }
    }

    public void Pause()
    {
        if (!m_disposed)
        {
            OpenALDebug.Start("Pausing sound");
            AL.SourcePause(m_sourceId);
            OpenALDebug.End("Pausing sound");
        }
    }

    public bool IsPlaying()
    {
        if (m_disposed)
            return false;

        OpenALDebug.Start("Checking if sound is playing");
        AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out int state);
        OpenALDebug.End("Checking if sound is playing");

        return (ALSourceState)state == ALSourceState.Playing;
    }

    public void Stop()
    {
        if (!m_disposed)
        {
            OpenALDebug.Start("Stopping sound source");
            AL.SourceStop(m_sourceId);
            OpenALDebug.End("Stopping sound source");
        }
    }

    public bool IsFinished()
    {
        if (m_disposed)
            return true;

        // For the future, maybe we should just track timestamps instead as
        // using "stopped" means we don't know if someone called Stop() or
        // if the sound fully finished.
        OpenALDebug.Start("Checking if sound finished playing");
        AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out int state);
        OpenALDebug.End("Checking if sound finished playing");

        return (ALSourceState)state == ALSourceState.Stopped;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;

        PerformDispose();
        GC.SuppressFinalize(this);
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        CacheFree();

        m_disposed = true;
    }

    public void CacheFree()
    {
        Completed?.Invoke(this, EventArgs.Empty);

        Owner.Unlink(this);
        OpenALDebug.Start("Deleting sound source");
        AL.DeleteSource(m_sourceId);
        OpenALDebug.End("Deleting sound source");

        DataCache.Instance.FreeAudioData(AudioData);
    }
}

using Helion.Audio.Impl.Components;
using Helion.Geometry.Vectors;
using Helion.World.Entities;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl;

public class OpenALAudioSource : IAudioSource
{
    private const float DefaultReference = 200f;
    private const float MaxAudibleDistance = 1200f;
    private const ALSourcef SourceRadius = (ALSourcef)0x1031;
    private const ALSourcei SourceDistanceModel = (ALSourcei)53248;
    private const ALSourcei SourceRelative = (ALSourcei)0x202;

    private AudioData m_audioData;

    public event EventHandler? Completed;

    public Vec3F Velocity { get; set; }

    public AudioData AudioData
    {
        get => m_audioData;
        set => m_audioData = value;
    }
    public OpenALAudioSourceManager Owner { get; private set; }
    public IAudioSource? Previous { get; set; }
    public IAudioSource? Next { get; set; }

    private int m_sourceId;
    private bool m_disposed;
    private float m_gain = 1f;

    public int ID => m_sourceId;

    public OpenALAudioSource(OpenALAudioSourceManager owner, OpenALBuffer buffer, in AudioData audioData)
    {
        Set(owner, buffer, audioData);
        Owner = owner;
        AudioData = audioData;
    }

    public void Set(OpenALAudioSourceManager owner, OpenALBuffer buffer, in AudioData audioData)
    {
        Owner = owner;
        AudioData = audioData;

        var rolloffFactor = 1f;
        var maxDistance = 65536.0f;
        var radius = audioData.SoundSource.GetSoundRadius();

        OpenALDebug.Start("Creating new source");
        m_sourceId = AL.GenSource();
        AL.Source(m_sourceId, ALSourcef.MinGain, 0.0f);
        if (owner.AudioSystem.Gain == 0)
        {
            // If sound effects gain is set to zero, we attenuate at the source to avoid muting the music.
            // Else, all volume attenuation is done at the listener.
            AL.Source(m_sourceId, ALSourcef.Gain, 0.0f);
        }
        else
        {
            AL.Source(m_sourceId, ALSourcef.Gain, audioData.Volume);
        }

        AL.DistanceModel(ALDistanceModel.None);
        AL.Source(m_sourceId, SourceRadius, radius);
        AL.Source(m_sourceId, ALSourcef.MaxDistance, maxDistance);
        AL.Source(m_sourceId, ALSourcef.RolloffFactor, rolloffFactor);
        AL.Source(m_sourceId, ALSourcef.Pitch, 1f);
        AL.Source(m_sourceId, ALSourceb.Looping, audioData.Loop);
        AL.Source(m_sourceId, ALSourcei.Buffer, buffer.BufferId);

        if (audioData.OffsetSeconds > 0)
        {
            var channels = AL.GetBuffer(m_sourceId, ALGetBufferi.Channels);
            var bitsPerSample = AL.GetBuffer(m_sourceId, ALGetBufferi.Bits);

            var offset = audioData.OffsetSeconds;
            var totalDuration = buffer.Bytes / Math.Max(buffer.SampleRate * channels * (bitsPerSample / 8f), 1);
            offset %= totalDuration;

            AL.Source(m_sourceId, ALSourcef.SecOffset, Math.Max(offset, 0));
        }

        if (audioData.Relative || audioData.Attenuation == Attenuation.None)
            SetRelative(true);

        OpenALDebug.End("Creating new source");
    }

    public void SetGain(double gain)
    {
        m_gain = (float)gain;
        OpenALDebug.Start("Setting sound gain");
        AL.Source(m_sourceId, ALSourcef.Gain, m_gain * m_audioData.Volume);
        OpenALDebug.End("Setting sound gain");
    }

    public void SetPosition(float x, float y, float z)
    {
        OpenALDebug.Start("Setting sound position");
        if (m_audioData.Relative)
            return;
        AL.Source(m_sourceId, ALSource3f.Position, x, y, z);
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

    public void SetGain(float gain)
    {
        OpenALDebug.Start("Setting sound gain");
        AL.Source(m_sourceId, ALSourcef.Gain, gain);
        OpenALDebug.End("Setting sound gain");
    }

    public void SetRelative(bool set)
    {
        m_audioData.Relative = set;
        if (set)
        {
            AL.Source(m_sourceId, SourceRelative, 1);
            AL.Source(m_sourceId, ALSource3f.Position, 0f, 0f, 0f);
        }
        else
        {
            AL.Source(m_sourceId, SourceRelative, 0);
        }
    }

    public Vec3F GetPosition()
    {
        OpenALDebug.Start("Getting sound position");
        AL.GetSource(m_sourceId, ALSource3f.Position, out Vector3 pos);
        OpenALDebug.End("Getting sound position");

        return new Vec3F(pos.X, pos.Y, pos.Z);
    }

    public void SetVelocity(float x, float y, float z)
    {
        OpenALDebug.Start("Setting sound velocity");
        AL.Source(m_sourceId, ALSource3f.Velocity, x, y, z);
        OpenALDebug.End("Setting sound velocity");
    }

    public Vec3F GetVelocity()
    {
        OpenALDebug.Start("Getting sound velocity");
        AL.GetSource(m_sourceId, ALSource3f.Velocity, out Vector3 vel);
        OpenALDebug.End("Getting sound velocity");

        return new Vec3F(vel.X, vel.Y, vel.Z);
    }

    public void SetOffsetSeconds(float offset)
    {
        AL.Source(m_sourceId, ALSourcef.SecOffset, offset);
    }

    public float GetOffsetSeconds()
    {
        return AL.GetSource(m_sourceId, ALSourcef.SecOffset);
    }

    public void Update(in UpdateParams updateParams)
    {
        if (m_audioData.Attenuation == Attenuation.None)
        {
            AL.Source(m_sourceId, ALSourcef.Gain, m_gain * m_audioData.Volume);
            return;
        }

        const float NoGain = 0.0001f;
        var dist = updateParams.DistanceFromListener * m_audioData.AttenuationFactor;
        if (dist > MaxAudibleDistance)
        {
            AL.Source(m_sourceId, ALSourcef.Gain, NoGain);
        }
        else
        { 
            // Doom's original linear scaling
            var linearGain = (MaxAudibleDistance - dist) / (MaxAudibleDistance - DefaultReference);
            // Push curve to dropoff faster to sound more appropriate
            var gain = Math.Clamp(linearGain * Math.Min(linearGain * 2f, linearGain), 0.005f, 1f);
            AL.Source(m_sourceId, ALSourcef.Gain, Math.Max(gain * m_gain * m_audioData.Volume, NoGain));
        }
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
        Owner = null!;
        AudioData = new();
        OpenALDebug.Start("Deleting sound source");
        AL.DeleteSource(m_sourceId);
        OpenALDebug.End("Deleting sound source");
    }
}

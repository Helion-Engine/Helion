using System;
using System.Numerics;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioSource : IAudioSource
    {
        private static readonly ALSourcef SourceRadius = (ALSourcef)0x1031;
        private static readonly ALSourcei SourceDistanceModel = (ALSourcei)53248; 

        private readonly ALAudioSourceManager m_owner;
        private readonly int m_sourceId;
        private bool m_disposed;

        public void SetPosition(in Vector3 pos)
        {
            AL.Source(m_sourceId, ALSource3f.Position, pos.X, pos.Y, pos.Z);
        }

        public Vector3 GetPosition()
        {
            OpenTK.Vector3 pos;
            AL.GetSource(m_sourceId, ALSource3f.Position, out pos);
            return new Vector3(pos.X, pos.Y, pos.Z);
        }

        public void SetVelocity(in Vector3 velocity)
        {
            AL.Source(m_sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
        }

        public Vector3 Velocity { get; set; }
        public object? SoundSource { get; set; }
        public bool Loop { get; set; }

        public ALAudioSource(ALAudioSourceManager owner, ALBuffer buffer, SoundParams soundParams)
        {
            m_owner = owner;
            m_sourceId = AL.GenSource();
            Loop = soundParams.Loop;

            if (soundParams.Attenuation == Attenuation.None)
            {
                // Max out the distance to prevent directional sound from taking effect
                AL.Source(m_sourceId, SourceRadius, 65536.0f);
                AL.Source(m_sourceId, ALSourcef.ReferenceDistance, 0.0f);
            }
            else
            {
                AL.Source(m_sourceId, ALSourcef.RolloffFactor, soundParams.RolloffFactor);
                AL.Source(m_sourceId, ALSourcef.ReferenceDistance, soundParams.ReferenceDistance);
                AL.Source(m_sourceId, SourceRadius, 128.0f);
                AL.Source(m_sourceId, ALSourcef.MaxDistance, soundParams.MaxDistance);
            }

            AL.Source(m_sourceId, ALSourceb.Looping, Loop);
            AL.Source(m_sourceId, ALSourcei.Buffer, buffer.BufferId);
        }

        ~ALAudioSource()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void Play()
        {
            if (!m_disposed)
                AL.SourcePlay(m_sourceId);
        }

        public bool IsPlaying()
        {
            if (m_disposed)
                return false;
            
            AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out int state);
            return (ALSourceState)state == ALSourceState.Playing;
        }

        public void Stop()
        {
            if (!m_disposed)
                AL.SourceStop(m_sourceId);
        }

        public bool IsFinished()
        {
            if (m_disposed)
                return true;
            
            // For the future, maybe we should just track timestamps instead as
            // using "stopped" means we don't know if someone called Stop() or
            // if the sound fully finished.
            AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out int state);
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
            
            Stop();
            m_owner.Unlink(this);
            AL.DeleteSource(m_sourceId);

            m_disposed = true;
        }
    }
}
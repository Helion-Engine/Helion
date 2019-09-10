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
        private readonly ALAudioSourceManager m_owner;
        private readonly int m_sourceId;
        private bool m_disposed;
        
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }

        public ALAudioSource(ALAudioSourceManager owner, ALBuffer buffer)
        {
            m_owner = owner;
            m_sourceId = AL.GenSource();
            AL.Source(m_sourceId, ALSourcei.Buffer, buffer.BufferId);
        }

        ~ALAudioSource()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
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
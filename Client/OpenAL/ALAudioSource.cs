using System;
using System.Numerics;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioSource : IAudioSource
    {
        private const float DefaultRolloff = 2.5f;
        private const float DefaultReference = 296.0f;
        private const float DefaultMaxDistance = (float)Constants.MaxSoundDistance;
        private const float DefaultRadius = 32.0f;
        private const ALSourcef SourceRadius = (ALSourcef)0x1031;
        private const ALSourcei SourceDistanceModel = (ALSourcei)53248;

        public Vector3 Velocity { get; set; }
        public AudioData AudioData { get; set; }
        private readonly ALAudioSourceManager m_owner;
        private int m_sourceId;
        private bool m_disposed;

        public int ID => m_sourceId;

        public ALAudioSource(ALAudioSourceManager owner, ALBuffer buffer, AudioData audioData, SoundParams soundParams)
        {
            m_owner = owner;
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

            ALExecutor.Run("Creating new source", () =>
            {
                m_sourceId = AL.GenSource();
                AL.Source(m_sourceId, ALSourcef.MinGain, 0.0f);
                AL.Source(m_sourceId, ALSourcef.RolloffFactor, rolloffFactor);
                AL.Source(m_sourceId, ALSourcef.ReferenceDistance, referenceDistance);
                AL.Source(m_sourceId, SourceRadius, radius);
                AL.Source(m_sourceId, ALSourcef.MaxDistance, maxDistance);
                AL.Source(m_sourceId, ALSourcef.Pitch, 1.0f);
                AL.Source(m_sourceId, ALSourceb.Looping, soundParams.Loop);
                AL.Source(m_sourceId, ALSourcei.Buffer, buffer.BufferId);
            });
        }

        public void SetPosition(Vector3 pos)
        {
            ALExecutor.Run("Setting sound position", () =>
            {
                AL.Source(m_sourceId, ALSource3f.Position, pos.X, pos.Y, pos.Z);
            });
        }

        public Vector3 GetPosition()
        {
            OpenTK.Vector3 pos = default;

            ALExecutor.Run("Getting sound position", () =>
            {
                AL.GetSource(m_sourceId, ALSource3f.Position, out pos);
            });

            return new Vector3(pos.X, pos.Y, pos.Z);
        }

        public void SetVelocity(Vector3 velocity)
        {
            ALExecutor.Run("Setting sound velocity", () =>
            {
                AL.Source(m_sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
            });
        }

        ~ALAudioSource()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override int GetHashCode() => m_sourceId;

        public override bool Equals(object? obj)
        {
            if (obj is ALAudioSource audioSource)
                return audioSource.m_sourceId == m_sourceId;

            return false;
        }

        public void Play()
        {
            if (!m_disposed)
            {
                ALExecutor.Run("Playing sound", () =>
                {
                    AL.SourcePlay(m_sourceId);
                });
            }
        }

        public void Pause()
        {
            if (!m_disposed)
            {
                ALExecutor.Run("Pausing sound", () =>
                {
                    AL.SourcePause(m_sourceId);
                });
            }
        }

        public bool IsPlaying()
        {
            if (m_disposed)
                return false;

            int state = 0;
            ALExecutor.Run("Checking if sound is playing", () =>
            {
                AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out state);
            });

            return (ALSourceState)state == ALSourceState.Playing;
        }

        public void Stop()
        {
            if (!m_disposed)
            {
                ALExecutor.Run("Stopping sound source", () =>
                {
                    AL.SourceStop(m_sourceId);
                });
            }
        }

        public bool IsFinished()
        {
            if (m_disposed)
                return true;

            // For the future, maybe we should just track timestamps instead as
            // using "stopped" means we don't know if someone called Stop() or
            // if the sound fully finished.
            int state = 0;
            ALExecutor.Run("Checking if sound finished playing", () =>
            {
                AL.GetSource(m_sourceId, ALGetSourcei.SourceState, out state);
            });

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

            m_owner.Unlink(this);
            ALExecutor.Run("Deleting sound source", () =>
            {
                AL.DeleteSource(m_sourceId);
            });

            m_disposed = true;
        }
    }
}
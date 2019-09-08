using System;
using System.Numerics;
using Helion.Audio;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class OpenALAudioSource : IAudioSource
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }

        ~OpenALAudioSource()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        public void Play()
        {
            // TODO
        }

        public void Stop()
        {
            // TODO
        }

        private void PerformDispose()
        {
            // TODO
        }
    }
}
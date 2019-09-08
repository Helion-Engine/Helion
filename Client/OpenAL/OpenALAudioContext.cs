using System;
using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class OpenALAudioContext : IAudioContext
    {
        public IAudioListener Listener { get; } = new OpenALAudioListener();

        public OpenALAudioContext(Config config, ArchiveCollection archiveCollection)
        {
            // TODO
        }

        ~OpenALAudioContext()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public IAudioSource Create(string sound)
        {
            OpenALAudioSource source = new OpenALAudioSource();
            return source;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            // TODO
        }
    }
}
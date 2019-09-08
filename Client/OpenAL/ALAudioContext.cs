using System;
using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioContext : IAudioContext
    {
        public IAudioListener Listener { get; } = new ALAudioListener();

        public ALAudioContext(Config config, ArchiveCollection archiveCollection)
        {
            // TODO
        }

        ~ALAudioContext()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public IAudioSource Create(string sound)
        {
            ALAudioSource source = new ALAudioSource();
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
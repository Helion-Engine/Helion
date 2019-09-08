using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class OpenALAudioSystem : IAudioSystem
    {
        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly HashSet<OpenALAudioContext> m_contexts = new HashSet<OpenALAudioContext>();
        
        public OpenALAudioSystem(Config config, ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_archiveCollection = archiveCollection;
        }

        ~OpenALAudioSystem()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }
        
        public IAudioContext CreateContext()
        {
            OpenALAudioContext context = new OpenALAudioContext(m_config, m_archiveCollection);
            m_contexts.Add(context);
            return context;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            // Since children contexts on disposing unlink themselves from us,
            // we don't want to be mutating the container while iterating over
            // it.
            m_contexts.ToList().ForEach(ctx => ctx.Dispose());
            Invariant(m_contexts.Empty(), "Disposal of AL audio context children should empty out of the context container");
        }
    }
}
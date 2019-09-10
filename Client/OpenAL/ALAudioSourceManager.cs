using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using MoreLinq;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioSourceManager : IAudioSourceManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IAudioListener Listener { get; } = new ALAudioListener();
        private readonly ArchiveCollection m_archiveCollection;
        private readonly ALAudioSystem m_owner;
        private readonly HashSet<ALAudioSource> m_sources = new HashSet<ALAudioSource>();
        private readonly Dictionary<string, ALBuffer> m_nameToBuffer = new Dictionary<string, ALBuffer>();

        public ALAudioSourceManager(ALAudioSystem owner, ArchiveCollection archiveCollection)
        {
            m_owner = owner;
            m_archiveCollection = archiveCollection;
        }

        ~ALAudioSourceManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public IAudioSource? Create(string sound)
        {
            ALBuffer? buffer = GetBuffer(sound);
            if (buffer == null)
                return null;
            
            ALAudioSource source = new ALAudioSource(this, buffer);
            m_sources.Add(source);
            return source;
        }

        private ALBuffer? GetBuffer(string sound)
        {
            string upperSound = sound.ToUpper();
            if (m_nameToBuffer.TryGetValue(upperSound, out ALBuffer? existingBuffer))
                return existingBuffer;

            Entry? entry = m_archiveCollection.Entries.FindByNamespace(upperSound, ResourceNamespace.Sounds);
            if (entry == null)
            {
                Log.Warn("Cannot find sound: {0}", sound);
                return null;
            }
            
            ALBuffer? buffer = ALBuffer.Create(entry.ReadData());
            if (buffer == null)
            {
                Log.Warn("Sound {0} is either corrupt or not a DMX sound", sound);
                return null;
            }
            
            m_nameToBuffer[upperSound] = buffer;
            return buffer;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        internal void Unlink(ALAudioSource source)
        {
            m_sources.Remove(source);
        }

        private void PerformDispose()
        {
            m_owner.Unlink(this);
            
            // We create a copy list because disposing will mutate the list
            // that it belongs to, since it has no idea if we're disposing it
            // manually or by disposal of its manager.
            m_sources.ToList().ForEach(src => src.Dispose());
            Invariant(m_sources.Empty(), "Disposal of AL audio source children should empty out of the context container");

            m_nameToBuffer.ForEach(pair => pair.Value.Dispose());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;
using NLog;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioSystem : IAudioSystem
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static bool PrintedALInfo;

        private readonly ArchiveCollection m_archiveCollection;
        private readonly HashSet<ALAudioSourceManager> m_sourceManagers = new HashSet<ALAudioSourceManager>();
        private readonly ALDevice m_alDevice;
        private readonly ALContext m_alContext;
        private readonly ISet<string> m_extensions = new HashSet<string>();

        public ALAudioSystem(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_alDevice = new ALDevice();
            m_alContext = new ALContext(m_alDevice);
            
            PrintOpenALInfo();
            DiscoverExtensions();
        }

        [Conditional("DEBUG")]
        public static void CheckForErrors()
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
                Fail($"Unexpected OpenAL error: {error}");
        }

        private static void PrintOpenALInfo()
        {
            if (PrintedALInfo)
                return;

            Log.Info("OpenAL v{0}", AL.Get(ALGetString.Version));
            Log.Info("OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
            Log.Info("OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
            Log.Info("OpenAL Extensions: {0}", AL.Get(ALGetString.Extensions).Split(' ').Length);

            PrintedALInfo = true;
        }

        ~ALAudioSystem()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public IAudioSourceManager CreateContext()
        {
            ALAudioSourceManager sourceManager = new ALAudioSourceManager(this, m_archiveCollection);
            m_sourceManagers.Add(sourceManager);
            return sourceManager;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        internal void Unlink(ALAudioSourceManager context)
        {
            m_sourceManagers.Remove(context);
        }

        private void DiscoverExtensions()
        {
            foreach (string extension in AL.Get(ALGetString.Extensions).Split(' '))
                m_extensions.Add(extension);
        }

        private void PerformDispose()
        {
            // Since children contexts on disposing unlink themselves from us,
            // we don't want to be mutating the container while iterating over
            // it.
            m_sourceManagers.ToList().ForEach(srcManager => srcManager.Dispose());
            Invariant(m_sourceManagers.Empty(), "Disposal of AL audio context children should empty out of the context container");

            m_alContext.Dispose();
            m_alDevice.Dispose();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
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

        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly HashSet<ALAudioContext> m_contexts = new HashSet<ALAudioContext>();
        private readonly ALDevice m_alDevice;
        private readonly ALContext m_alContext;
        private readonly ISet<string> m_extensions = new HashSet<string>();

        public ALAudioSystem(Config config, ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_archiveCollection = archiveCollection;
            m_alDevice = new ALDevice();
            m_alContext = new ALContext(m_alDevice);
            
            PrintOpenALInfo();
            DiscoverExtensions();
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
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }
        
        public IAudioContext CreateContext()
        {
            ALAudioContext context = new ALAudioContext(m_config, m_archiveCollection);
            m_contexts.Add(context);
            return context;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
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
            m_contexts.ToList().ForEach(ctx => ctx.Dispose());
            Invariant(m_contexts.Empty(), "Disposal of AL audio context children should empty out of the context container");

            m_alContext.Dispose();
            m_alDevice.Dispose();
        }
    }
}
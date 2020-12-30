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
        public event EventHandler? DeviceChanging;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool PrintedALInfo;

        public IMusicPlayer Music { get; }
        private readonly ArchiveCollection m_archiveCollection;
        private readonly HashSet<ALAudioSourceManager> m_sourceManagers = new HashSet<ALAudioSourceManager>();
        private readonly ISet<string> m_extensions = new HashSet<string>();
        private ALDevice m_alDevice;
        private ALContext m_alContext;

        public ALAudioSystem(ArchiveCollection archiveCollection, string deviceName, IMusicPlayer musicPlayer)
        {
            m_archiveCollection = archiveCollection;
            m_alDevice = new ALDevice(deviceName);
            m_alContext = new ALContext(m_alDevice);
            Music = musicPlayer;

            PrintOpenALInfo();
            DiscoverExtensions();
        }

        public IList<string> GetDeviceNames()
        {
            IList<string> devices = Alc.GetString(IntPtr.Zero, AlcGetStringList.AllDevicesSpecifier);
            devices.Insert(0, IAudioSystem.DefaultAudioDevice);
            return devices;
        }

        public string GetDeviceName()
        {
            return m_alDevice.DeviceName;
        }

        public void SetDevice(string deviceName)
        {
            DeviceChanging?.Invoke(this, EventArgs.Empty);

            m_alContext.Dispose();
            m_alDevice.Dispose();

            m_alDevice = new ALDevice(deviceName);
            m_alContext = new ALContext(m_alDevice);
        }

        public void SetVolume(float volume)
        {
            AL.Listener(ALListenerf.Gain, volume);
        }

        [Conditional("DEBUG")]
        public static void CheckForErrors()
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
                Fail($"Unexpected OpenAL error: {error} (reason: {AL.GetErrorString(error)})");
        }

        private void PrintOpenALInfo()
        {
            if (PrintedALInfo)
                return;

            Log.Info("OpenAL v{0}", AL.Get(ALGetString.Version));
            Log.Info("OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
            Log.Info("OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
            Log.Info("OpenAL Extensions: {0}", AL.Get(ALGetString.Extensions).Split(' ').Length);

            foreach (string device in GetDeviceNames())
                Log.Info($"Device: {device}");

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
            GC.SuppressFinalize(this);
            PerformDispose();
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
            Music.Dispose();
        }
    }
}
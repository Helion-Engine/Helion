using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Audio.Impl.Components;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using NLog;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl
{
    public class OpenALAudioSystem : IAudioSystem
    {
        public event EventHandler? DeviceChanging;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool PrintedALInfo;

        public IMusicPlayer Music { get; }
        private readonly ArchiveCollection m_archiveCollection;
        private readonly HashSet<OpenALAudioSourceManager> m_sourceManagers = new();
        private readonly ISet<string> m_extensions = new HashSet<string>();
        private OpenALDevice m_alDevice;
        private OpenALContext m_alContext;

        public OpenALAudioSystem(Config config, ArchiveCollection archiveCollection, IMusicPlayer musicPlayer)
        {
            m_archiveCollection = archiveCollection;
            m_alDevice = new OpenALDevice(config.Audio.Device);
            m_alContext = new OpenALContext(m_alDevice);
            Music = musicPlayer;

            PrintOpenALInfo();
            DiscoverExtensions();
        }

        public IList<string> GetDeviceNames()
        {
            IList<string> devices = ALC.GetString(m_alDevice.Device, AlcGetStringList.AllDevicesSpecifier);
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

            m_alDevice = new OpenALDevice(deviceName);
            m_alContext = new OpenALContext(m_alDevice);
        }

        public void SetVolume(double volume)
        {
            AL.Listener(ALListenerf.Gain, (float)volume);
        }

        [Conditional("DEBUG")]
        public static void CheckForErrors(string debugInfo = "", params object[] objs)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                string reason = string.Format(debugInfo, objs);
                Fail($"Unexpected OpenAL error: {error} (reason: {AL.GetErrorString(error)}) {reason}");
            }
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

        ~OpenALAudioSystem()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public IAudioSourceManager CreateContext()
        {
            OpenALAudioSourceManager sourceManager = new(this, m_archiveCollection);
            m_sourceManagers.Add(sourceManager);
            return sourceManager;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        internal void Unlink(OpenALAudioSourceManager context)
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

using System;
using Helion.Audio;
using Helion.Util;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL.Components
{
    public class ALDevice : IDisposable
    {
        public string DeviceName { get; private set; } = "UnknownName";
        internal IntPtr Device;

        public ALDevice()
        {
            CreateDefault();
        }

        public ALDevice(string deviceName)
        {
            DeviceName = deviceName;

            if (!string.IsNullOrEmpty(deviceName))
            {
                if (deviceName.Equals(IAudioSystem.DefaultAudioDevice, StringComparison.OrdinalIgnoreCase))
                    CreateDefault();
                else
                    Device = Alc.OpenDevice(deviceName);
            }

            if (Device == IntPtr.Zero)
                CreateDefault();
        }

        private void CreateDefault()
        {
            DeviceName = IAudioSystem.DefaultAudioDevice;
            Device = Alc.OpenDevice(null);
            if (Device == IntPtr.Zero)
                throw new HelionException("Unable to access OpenAL device");
        }

        ~ALDevice()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            Alc.CloseDevice(Device);
        }
    }
}
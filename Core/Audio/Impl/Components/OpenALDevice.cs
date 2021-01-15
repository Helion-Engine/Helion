using System;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALDevice : IDisposable
    {
        public string DeviceName { get; private set; }
        public string OpenALDeviceName { get; private set; }
        internal ALDevice Device;

        public OpenALDevice()
        {
            DeviceName = string.Empty;
            CreateDefault();
        }

        public OpenALDevice(string deviceName)
        {
            DeviceName = deviceName;

            if (!string.IsNullOrEmpty(deviceName))
            {
                if (deviceName.Equals(IAudioSystem.DefaultAudioDevice, StringComparison.OrdinalIgnoreCase))
                {
                    CreateDefault();
                }
                else
                {
                    Device = ALC.OpenDevice(deviceName);
                    OpenALDeviceName = ALC.GetString(Device, AlcGetString.AllDevicesSpecifier);
                }
            }

            if (Device == IntPtr.Zero)
                CreateDefault();
        }

        private void CreateDefault()
        {
            DeviceName = IAudioSystem.DefaultAudioDevice;
            Device = ALC.OpenDevice(null);
            if (Device == IntPtr.Zero)
                throw new Exception("Unable to access OpenAL device");

            OpenALDeviceName = ALC.GetString(Device, AlcGetString.AllDevicesSpecifier);
        }

        ~OpenALDevice()
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
            // TODO: Not checking this right now since it should not be setting errors (but is).
            // TODO: Appears to fail if we did not dispose all buffers/contexts.
            ALC.CloseDevice(Device);
        }
    }
}

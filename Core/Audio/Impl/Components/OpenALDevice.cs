using System;
using Helion.Util;
using Helion.Util.Extensions;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALDevice : IDisposable
    {
        public string DeviceName { get; private set; }
        internal readonly ALDevice Device = ALDevice.Null;

        public OpenALDevice()
        {
            DeviceName = string.Empty;
            CreateDefault();
        }

        public OpenALDevice(string deviceName)
        {
            DeviceName = deviceName;

            if (!deviceName.Empty())
            {
                if (deviceName.Equals(IAudioSystem.DefaultAudioDevice, StringComparison.OrdinalIgnoreCase))
                    CreateDefault();
                else
                    Device = ALC.OpenDevice(deviceName);
            }

            if (Device == ALDevice.Null)
                Device = CreateDefault();
        }

        private ALDevice CreateDefault()
        {
            DeviceName = IAudioSystem.DefaultAudioDevice;
            ALDevice device = ALC.OpenDevice(DeviceName);
            if (device == IntPtr.Zero)
                throw new Exception("Unable to access OpenAL device");

            return device;
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

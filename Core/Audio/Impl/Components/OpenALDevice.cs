using System;
using Helion.Util.Configs;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALDevice : IDisposable
    {
        public readonly string DeviceName;
        internal readonly ALDevice Device;

        public OpenALDevice(Config config)
        {
            DeviceName = FindDeviceName(config);
            Device = ALC.OpenDevice(DeviceName);

            if (Device == ALDevice.Null)
                throw new Exception($"Unable to open device: {DeviceName}");
        }

        private string FindDeviceName(Config config)
        {
            // TODO: We can use the config here if we are sure of the type we want.

            foreach (string name in ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier))
                if (name.Contains("OpenAL Soft"))
                    return name;

            return ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
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

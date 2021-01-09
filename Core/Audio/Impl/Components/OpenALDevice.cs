using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Configs;
using Helion.Util.Extensions;
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

        private static string FindDeviceName(Config config)
        {
            List<string> devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier).ToList();

            // If the user requests something specific, try to give it to them.
            if (!config.Audio.Device.Value.Empty())
                foreach (string name in devices)
                    if (name.Contains(config.Audio.Device))
                        return name;

            // If we can't find the one we want, or if we don't care, try to
            // find the best one on the system. Usually this is one that has
            // the word 'Soft' in it.
            foreach (string name in devices)
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

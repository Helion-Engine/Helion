using System;
using Helion.Util;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL.Components
{
    public class ALDevice : IDisposable
    {
        internal readonly IntPtr Device;
        
        public ALDevice()
        {
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
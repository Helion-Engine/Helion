using System;
using Helion.Util;
using OpenTK;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL.Components
{
    public class ALContext : IDisposable
    {
        private readonly ContextHandle m_context;
        private readonly int[] m_attributeList = Array.Empty<int>();
        private bool m_disposed;

        public ALContext(ALDevice device)
        {
            m_context = Alc.CreateContext(device.Device, m_attributeList);
            if (m_context.Handle == IntPtr.Zero)
                throw new HelionException("Unable to access OpenAL device");

            Alc.MakeContextCurrent(m_context);
        }

        ~ALContext()
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
            if (m_disposed)
                return;

            // The spec tells us we have to set a null context before
            // destroying a valid one.
            Alc.MakeContextCurrent(ContextHandle.Zero);
            // TODO: Not checking this right now since even NULL causes this to error.
            Alc.DestroyContext(m_context);

            m_disposed = true;
        }
    }
}
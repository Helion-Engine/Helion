using System;
using Helion.Util;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALContext : IDisposable
    {
        private readonly ALContext m_context;
        private readonly int[] m_attributeList = Array.Empty<int>();
        private bool m_disposed;

        public OpenALContext(OpenALDevice device)
        {
            m_context = ALC.CreateContext(device.Device, m_attributeList);
            if (m_context.Handle == IntPtr.Zero)
                throw new HelionException("Unable to access OpenAL device");

            ALC.MakeContextCurrent(m_context);
        }

        ~OpenALContext()
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
            ALC.MakeContextCurrent(ALContext.Null);
            // TODO: Not checking this right now since even NULL causes this to error.
            ALC.DestroyContext(m_context);

            m_disposed = true;
        }
    }
}

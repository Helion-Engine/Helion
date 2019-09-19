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
        private readonly int[] m_attributeList = new int[0];

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
            Alc.MakeContextCurrent(m_context);
            Alc.DestroyContext(m_context);
        }
    }
}
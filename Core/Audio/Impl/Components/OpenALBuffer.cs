using System;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALBuffer : IDisposable
    {
        public int BufferId;

        private OpenALBuffer(int sampleRate, Span<byte> sampleData)
        {
            OpenALDebug.Start("Creating buffer");
            BufferId = AL.GenBuffer();
            OpenALDebug.End("Creating buffer");

            // Note: We only support DMX sounds currently!
            OpenALDebug.Start("Setting buffer data");
            AL.BufferData(BufferId, ALFormat.Mono8, sampleData, sampleRate);
            OpenALDebug.End("Setting buffer data");
        }

        ~OpenALBuffer()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public static OpenALBuffer? Create(byte[] data)
        {
            if (AudioHelper.TryReadDoomSound(data, out int sampleRate, out Span<byte> sampleData))
                return new OpenALBuffer(sampleRate, sampleData);
            return null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            OpenALDebug.Start("Deleting sound buffer");
            AL.DeleteBuffer(BufferId);
            OpenALDebug.End("Deleting sound buffer");
        }
    }
}

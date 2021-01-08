using System;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components
{
    public class OpenALBuffer : IDisposable
    {
        public int BufferId;

        private OpenALBuffer(int sampleRate, byte[] sampleData)
        {
            OpenALExecutor.Run("Creating buffer", () =>
            {
                BufferId = AL.GenBuffer();
            });

            OpenALExecutor.Run("Setting buffer data", () =>
            {
                // Note: We only support DMX sounds currently!
                AL.BufferData(BufferId, ALFormat.Mono8, sampleData, sampleRate);
            });
        }

        ~OpenALBuffer()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public static OpenALBuffer? Create(byte[] data)
        {
            if (AudioHelper.TryReadDoomSound(data, out int sampleRate, out byte[] sampleData))
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
            OpenALExecutor.Run("Deleting sound buffer", () =>
            {
                AL.DeleteBuffer(BufferId);
            });
        }
    }
}

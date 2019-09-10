using System;
using Helion.Audio;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL.Components
{
    public class ALBuffer : IDisposable
    {
        public readonly int BufferId;
        
        private ALBuffer(int sampleRate, byte[] sampleData)
        {
            BufferId = AL.GenBuffer();
            // Note: We only support DMX sounds currently!
            AL.BufferData(BufferId, ALFormat.Mono8, sampleData, sampleData.Length, sampleRate);
        }

        ~ALBuffer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }
        
        public static ALBuffer? Create(byte[] data)
        {
            if (AudioHelper.TryReadDoomSound(data, out int sampleRate, out byte[] sampleData))
                return new ALBuffer(sampleRate, sampleData);
            return null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            AL.DeleteBuffer(BufferId);
        }
    }
}
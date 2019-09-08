using System;
using System.Numerics;

namespace Helion.Audio
{
    public interface IAudioSource : IDisposable
    {
        Vector3 Position { get; set; }
        Vector3 Velocity { get; set; }
        
        void Play();
        void Stop();
    }
}
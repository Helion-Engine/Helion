using System;

namespace Helion.Audio
{
    public interface IAudioContext : IDisposable
    {
        IAudioListener Listener { get; }
        
        IAudioSource Create(string sound);
    }
}
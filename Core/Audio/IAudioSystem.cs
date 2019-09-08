using System;

namespace Helion.Audio
{
    /// <summary>
    /// The main top level audio system that delivers all of the audio to the
    /// application.
    /// </summary>
    public interface IAudioSystem : IDisposable
    {
        IAudioContext CreateContext();
    }
}
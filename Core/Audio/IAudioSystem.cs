using System;

namespace Helion.Audio
{
    /// <summary>
    /// The main top level audio system that delivers all of the audio to the
    /// application.
    /// </summary>
    public interface IAudioSystem : IDisposable
    {
        /// <summary>
        /// Creates a new audio context. See <see cref="IAudioContext"/> for
        /// more information on how to use this.
        /// </summary>
        /// <returns>A newly created audio context.</returns>
        IAudioContext CreateContext();
    }
}
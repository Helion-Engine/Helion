using System;

namespace Helion.Audio
{
    /// <summary>
    /// Plays music from sound file data.
    /// </summary>
    public interface IMusicPlayer : IDisposable
    {
        /// <summary>
        /// The data to play.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>True if it succeeded and is playing, false if there was an
        /// error loading or playing the track.</returns>
        bool Play(byte[] data);

        /// <summary>
        /// Stops playing the music.
        /// </summary>
        void Stop();
    }
}

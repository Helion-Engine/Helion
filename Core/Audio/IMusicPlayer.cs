using System;

namespace Helion.Audio;

/// <summary>
/// Plays music from sound file data.
/// </summary>
public interface IMusicPlayer : IDisposable
{
    /// <summary>
    /// Sets the volume to the value provided. 0.0 is off, and 1.0 is fully
    /// on. Any value outside of this range will be clamped to [0.0, 1.0].
    /// </summary>
    /// <param name="volume">The volume. Should be in [0.0, 1.0].</param>
    void SetVolume(float volume);

    /// <summary>
    /// The data to play.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="loop">True if it should loop, false if not.</param>
    /// <returns>True if it succeeded and is playing, false if there was an
    /// error loading or playing the track.</returns>
    /// <param name="ignoreAlreadyPlaying">If true and the currently playing midi data
    /// matches then this Play call will be ignored.</param>
    bool Play(byte[] data, bool loop = true, bool ignoreAlreadyPlaying = true);

    /// <summary>
    /// Stops playing the music.
    /// </summary>
    void Stop();
}


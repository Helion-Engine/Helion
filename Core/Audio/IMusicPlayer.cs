using System;

namespace Helion.Audio;

[Flags]
public enum MusicPlayerOptions
{
    None,
    Loop = 1,
    IgnoreAlreadyPlaying = 2
}

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
    /// <param name="options">Player options.</param>
    bool Play(byte[] data, MusicPlayerOptions options = MusicPlayerOptions.Loop | MusicPlayerOptions.IgnoreAlreadyPlaying);

    /// <summary>
    /// Stops playing the music.
    /// </summary>
    void Stop();

    /// <summary>
    /// Enables or disables the music system.  If disabled, the system will ignore requests to play music.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Ask the music player to stop playback temporarily and discard any outputs it is currently using
    /// </summary>
    void OutputChanging();

    /// <summary>
    /// Ask the music player to resume playback, possibly on a different output than it was using before
    /// </summary>
    void OutputChanged();
}

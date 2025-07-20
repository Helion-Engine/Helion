using System;

namespace Helion.Audio;

[Flags]
public enum MusicPlayerOptions
{
    None,
    Loop = 1,
    IgnoreAlreadyPlaying = 2,
    Reload = 4
}

/// <summary>
/// Plays music from sound file data.
/// </summary>
public interface IMusicPlayer : IDisposable
{
    /// <summary>
    /// Notify the music player that the volume settings have changed and it should adjust its volume
    /// </summary>
    void SetVolume();

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

using System;
using System.Collections.Generic;

namespace Helion.Audio
{
    /// <summary>
    /// The main top level audio system that delivers all of the audio to the
    /// application.
    /// </summary>
    public interface IAudioSystem : IDisposable
    {
        public static readonly string DefaultAudioDevice = "Default";

        /// <summary>
        /// Requests that the subsystem check for any errors, and throw an
        /// exception if any are found. Intended for debugging only.
        /// </summary>
        void ThrowIfErrorCheckFails();

        /// <summary>
        /// Creates a new audio context. See <see cref="IAudioSourceManager"/> for
        /// more information on how to use this.
        /// </summary>
        /// <returns>A newly created audio context.</returns>
        IAudioSourceManager CreateContext();

        event EventHandler DeviceChanging;

        IList<string> GetDeviceNames();
        string GetDeviceName();

        /// <summary>
        /// Sets the volume.
        /// </summary>
        /// <param name="volume">Volume in range of 0.0 to 1.0.</param>
        void SetVolume(double volume);

        /// <summary>
        /// <param name="deviceName">String of the audio device to set. Use DefaultAudioDevice property to set default audio device.</param>
        /// </summary>
        void SetDevice(string deviceName);

        /// <summary>
        /// The music player.
        /// </summary>
        IMusicPlayer Music { get; }
    }
}
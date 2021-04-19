using System;
using Helion.Geometry.Vectors;

namespace Helion.Audio
{
    /// <summary>
    /// A source of audio that can be played.
    /// </summary>
    /// <remarks>
    /// Supports a position and velocity so that we can attach sounds to actors
    /// in a world. This will be interpolated by the implementation to give us
    /// certain effects that we wouldn't have otherwise.
    /// Note that this is to be safe to dispose more than once. Trying to do a
    /// second (or more) disposal is okay and should not have any affects.
    /// </remarks>
    public interface IAudioSource : IDisposable
    {
        public event EventHandler? Completed;

        /// <summary>
        /// The location this audio source is to be played it. This is in world
        /// coordinates.
        /// </summary>
        void SetPosition(Vec3F pos);

        Vec3F GetPosition();

        /// <summary>
        /// The velocity (in map units) of the audio source.
        /// </summary>
        void SetVelocity(Vec3F velocity);

        AudioData AudioData { get; set; }

        /// <summary>
        /// Starts playing the sound.
        /// </summary>
        void Play();

        /// <summary>
        /// Checks if the sound is playing currently.
        /// </summary>
        /// <returns>True if it's playing, false if not.</returns>
        bool IsPlaying();

        /// <summary>
        /// Stops playing the sound.
        /// </summary>
        void Stop();

        /// <summary>
        /// Pauses playing the sound.
        /// </summary>
        void Pause();

        /// <summary>
        /// Checks whether the sound is finished playing and is eligible to be
        /// removed by whatever is managing this object.
        /// </summary>
        bool IsFinished();

        /// <summary>
        /// Intended for DataCache FreeAudioSource. Should cleanup all resources but
        /// not actually dispose the object so it can be used again from the cache.
        /// </summary>
        void CacheFree();
    }
}
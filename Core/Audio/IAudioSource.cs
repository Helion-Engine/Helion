using System;
using Helion.Geometry.Vectors;

namespace Helion.Audio;

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

    void SetPosition(float x, float y, float z);
    Vec3F GetPosition();

    void SetVelocity(float x, float y, float z);
    Vec3F GetVelocity();

    float GetPitch();
    void SetPitch(float pitch);

    AudioData AudioData { get; set; }

    void Play();

    bool IsPlaying();

    void Stop();

    void Pause();

    bool IsFinished();

    /// <summary>
    /// Intended for DataCache FreeAudioSource. Should cleanup all resources but
    /// not actually dispose the object so it can be used again from the cache.
    /// </summary>
    void CacheFree();

    IAudioSource? Previous { get; set; }
    IAudioSource? Next { get; set; }
}

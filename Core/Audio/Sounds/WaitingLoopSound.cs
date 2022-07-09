using Helion.Geometry.Vectors;
using System;

namespace Helion.Audio.Sounds
{
    internal class WaitingLoopSound : IAudioSource
    {
        public AudioData AudioData { get; set; }
        public SoundParams SoundParams { get; set; }
        public Vec3D? Position { get; set; }
        public Vec3D? Velocity { get; set; }

        public event EventHandler? Completed;

        public WaitingLoopSound(AudioData audioData, SoundParams soundParams, Vec3D? position, Vec3D? velocity)
        {
            AudioData = audioData;
            SoundParams = soundParams;
            Position = position;
            Velocity = velocity;
        }

        public float GetPitch() => 0;
        public Vec3F GetPosition() => Vec3F.Zero;
        public bool IsFinished() => false;
        public bool IsPlaying() => false;

        public void CacheFree()
        {
        }

        public void Dispose()
        {
        }

        public void Pause()
        {
        }

        public void Play()
        {
        }

        public void SetPitch(float pitch)
        {
        }

        public void SetPosition(Vec3F pos)
        {
        }

        public void SetVelocity(Vec3F velocity)
        {
        }

        public void Stop()
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}

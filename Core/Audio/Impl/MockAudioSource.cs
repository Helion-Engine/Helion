using Helion.Geometry.Vectors;
using System;

namespace Helion.Audio.Impl
{
    internal class MockAudioSource : IAudioSource
    {
        public AudioData AudioData { get; set; }

        public event EventHandler? Completed;

        private bool m_playing;
        private bool m_finished;
        private float m_pitch;
        private Vec3F m_position;
        private int m_playTicks;

        public MockAudioSource(in AudioData audioData, int ticksToPlay)
        {
            AudioData = audioData;
            m_playTicks = ticksToPlay;
        }

        public void Dispose()
        {

        }

        public void CacheFree()
        {

        }

        public void Tick()
        {
            if (m_playing)
                m_playTicks--;

            if (m_playTicks <= 0)
                Complete();
        }

        public float GetPitch() => m_pitch;

        public Vec3F GetPosition() => m_position;
        public Vec3F GetVelocity() => Vec3F.Zero;

        public bool IsFinished() => m_finished;

        public bool IsPlaying() => m_playing;

        public void Pause()
        {
            m_playing = false;
        }

        public void Play()
        {
            m_playing = true;
        }

        private void Complete()
        {
            if (!m_playing)
                return;

            m_playing = false;
            m_finished = true;
            Completed?.Invoke(this, EventArgs.Empty);
        }

        public void SetPitch(float pitch)
        {
            m_pitch = pitch;
        }

        public void SetPosition(Vec3F pos)
        {
            m_position = pos;
        }

        public void SetVelocity(Vec3F velocity)
        {

        }

        public void Stop()
        {
            if (m_playing)
                Complete();
        }
    }
}

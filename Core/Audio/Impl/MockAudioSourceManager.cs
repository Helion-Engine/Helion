using Helion.Geometry.Vectors;
using System;
using System.Collections.Generic;

namespace Helion.Audio.Impl
{
    public class MockAudioSourceManager : IAudioSourceManager
    {
        private readonly LinkedList<MockAudioSource> m_audioSources = new();

        public void CacheSound(string name)
        {
            
        }

        public IAudioSource? Create(string sound, in AudioData audioData)
        {
            var audioSource = new MockAudioSource(audioData, 35);
            m_audioSources.AddLast(audioSource);
            return audioSource;
        }

        public void DeviceChanging()
        {
            
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void PlayGroup(IEnumerable<IAudioSource> audioSources)
        {
            foreach (var audioSource in audioSources)
                audioSource.Play();
        }

        public void SetListener(Vec3D pos, double angle, double pitch)
        {
            
        }

        public void Tick()
        {
            var node = m_audioSources.First;
            while (node != null)
            {
                var nextNode = node.Next;
                node.Value.Tick();
                if (node.Value.IsFinished())
                    m_audioSources.Remove(node);
                node = nextNode;
            }
        }
    }
}

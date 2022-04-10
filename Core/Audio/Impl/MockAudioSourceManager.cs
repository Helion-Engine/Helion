using Helion.Geometry.Vectors;
using System.Collections.Generic;

namespace Helion.Audio.Impl
{
    public class MockAudioSourceManager : IAudioSourceManager
    {
        public void CacheSound(string name)
        {
            
        }

        public IAudioSource? Create(string sound, AudioData audioData, SoundParams soundParams)
        {
            return null;
        }

        public void DeviceChanging()
        {
            
        }

        public void Dispose()
        {
            
        }

        public void PlayGroup(IEnumerable<IAudioSource> audioSources)
        {
            
        }

        public void SetListener(Vec3D pos, double angle, double pitch)
        {
            
        }
    }
}

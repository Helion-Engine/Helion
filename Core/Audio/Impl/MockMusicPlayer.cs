using Helion.Audio;

namespace Helion.Audio.Impl
{
    public class MockMusicPlayer : IMusicPlayer
    {
        public void Dispose()
        {

        }

        public bool Play(byte[] data, MusicPlayerOptions options)
        {
            return true;
        }

        public void SetVolume(float volume)
        {

        }

        public void Stop()
        {

        }
    }
}

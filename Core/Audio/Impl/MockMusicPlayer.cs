using Helion.Audio;
using Helion.Resources.Archives.Entries;

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

        public bool Play(Entry entry, MusicPlayerOptions options)
        {
            return true;
        }

        public void SetVolume()
        {

        }

        public void Stop()
        {

        }

        public void OutputChanging()
        {

        }

        public void OutputChanged()
        {

        }

        public void ClearCachedData()
        {

        }

        public bool Enabled { get; set; }
    }
}

using Helion.Audio;
using Helion.Resources.Archives.Entries;

namespace Helion.Audio.Impl
{
    public class MockMusicPlayer : IMusicPlayer
    {
        public void Dispose()
        {

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

        public void CacheMusicEntry(Entry entry)
        {

        }

        public bool Enabled { get; set; }
    }
}

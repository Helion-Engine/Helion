namespace Helion.Client.Music
{
    using Helion.Audio;

    internal class ZMusicPlayer : IMusicPlayer
    {
        public void Dispose()
        {

        }

        public bool Play(byte[] data, MusicPlayerOptions options = MusicPlayerOptions.Loop | MusicPlayerOptions.IgnoreAlreadyPlaying)
        {
            return false;
        }

        public void SetVolume(float volume)
        {

        }

        public void Stop()
        {

        }
    }
}

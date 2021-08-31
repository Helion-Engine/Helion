namespace Helion.Client
{
    public partial class Client
    {
        private void RegisterConfigChanges()
        {
            m_config.Audio.MusicVolume.OnChanged += MusicVolume_OnChanged;
            m_config.Audio.SoundVolume.OnChanged += SoundVolume_OnChanged;
        }
        
        private void UnregisterConfigChanges()
        {
            m_config.Audio.MusicVolume.OnChanged -= MusicVolume_OnChanged;
            m_config.Audio.SoundVolume.OnChanged -= SoundVolume_OnChanged;
        }

        private void SoundVolume_OnChanged(object? sender, double volume) =>
            m_audioSystem.SetVolume(volume);

        private void MusicVolume_OnChanged(object? sender, double volume) =>
            m_audioSystem.Music.SetVolume((float)volume);
    }
}

using Helion.World.Entities.Players;

namespace Helion.Client;

public partial class Client
{
    private void RegisterConfigChanges()
    {
        m_config.Audio.MusicVolume.OnChanged += MusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged += SoundVolume_OnChanged;
        m_config.Mouse.Look.OnChanged += Look_OnChanged;
    }

    private void Look_OnChanged(object? sender, bool set)
    {
        if (m_layerManager.WorldLayer == null || set)
            return;

        m_layerManager.WorldLayer.AddCommand(TickCommands.CenterView);
    }

    private void UnregisterConfigChanges()
    {
        m_config.Audio.MusicVolume.OnChanged -= MusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged -= SoundVolume_OnChanged;
        m_config.Mouse.Look.OnChanged -= Look_OnChanged;
    }

    private void SoundVolume_OnChanged(object? sender, double volume) =>
        m_audioSystem.SetVolume(volume);

    private void MusicVolume_OnChanged(object? sender, double volume) =>
        m_audioSystem.Music.SetVolume((float)volume);
}

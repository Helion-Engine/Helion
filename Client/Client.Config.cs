using Helion.Geometry;
using Helion.Util.Configs.Components;
using Helion.World.Entities.Players;
using OpenTK.Windowing.Common;

namespace Helion.Client;

public partial class Client
{
    private void RegisterConfigChanges()
    {
        m_config.Audio.MusicVolume.OnChanged += MusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged += SoundVolume_OnChanged;
        m_config.Mouse.Look.OnChanged += Look_OnChanged;

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Dimension.OnChanged += WindowDimension_OnChanged;
        m_config.Window.Border.OnChanged += WindowBorder_OnChanged;
        m_config.Window.Display.OnChanged += WindowDisplay_OnChanged;
    }

    private void WindowDisplay_OnChanged(object? sender, int display) =>
        m_window.SetDisplay(display);

    private void WindowBorder_OnChanged(object? sender, WindowBorder border) =>
        m_window.SetBorder(border);

    private void WindowDimension_OnChanged(object? sender, Dimension dimension) =>
        m_window.SetDimension(dimension);

    private void WindowState_OnChanged(object? sender, RenderWindowState state) =>
        m_window.SetWindowState(state);

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

using Helion.Client.Music;
using Helion.Geometry;
using Helion.Util.Configs.Components;
using Helion.World.Entities.Players;
using OpenTK.Windowing.Common;
using System;
using System.IO;

namespace Helion.Client;

public partial class Client
{
    private void RegisterConfigChanges()
    {
        m_config.Audio.MusicVolume.OnChanged += MusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged += SoundVolume_OnChanged;
        m_config.Audio.Volume.OnChanged += Volume_OnChanged;
        m_config.Audio.SoundFontFile.OnChanged += SoundFont_OnChanged;
        m_config.Audio.Synthesizer.OnChanged += this.UseOPLEmulation_OnChanged;
        m_config.Mouse.Look.OnChanged += Look_OnChanged;

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Dimension.OnChanged += WindowDimension_OnChanged;
        m_config.Window.Border.OnChanged += WindowBorder_OnChanged;
        m_config.Window.Display.OnChanged += WindowDisplay_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged += WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged += WindowVirtualDimension_OnChanged;
        m_config.Hud.AutoScale.OnChanged += AutoScale_OnChanged;

        CalculateHudScale();
    }

    private void CalculateHudScale()
    {
        if (!m_config.Hud.AutoScale)
            return;

        int ratio = Math.Clamp((int)Math.Ceiling(m_window.Size.Y / 799.0), 1, 10);
        m_config.Hud.Scale.Set(ratio);
    }

    private void AutoScale_OnChanged(object? sender, bool set)
    {
        if (set)
            CalculateHudScale();
    }

    private void WindowDisplay_OnChanged(object? sender, int display) =>
        m_window.SetDisplay(display);

    private void WindowVirtualEnable_OnChanged(object? sender, bool set) =>
        CalculateHudScale();

    private void WindowVirtualDimension_OnChanged(object? sender, Dimension dim) =>
        CalculateHudScale();

    private void WindowBorder_OnChanged(object? sender, WindowBorder border)
    {
        m_window.SetBorder(border);
    }

    private void WindowDimension_OnChanged(object? sender, Dimension dimension)
    {
        m_window.SetDimension(dimension);
        CalculateHudScale();
    }

    private void WindowState_OnChanged(object? sender, RenderWindowState state)
    {
        m_window.SetWindowState(state);
        CalculateHudScale();
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

    private void SoundVolume_OnChanged(object? sender, double volume) => UpdateVolume();

    private void MusicVolume_OnChanged(object? sender, double volume) => UpdateVolume();

    private void Volume_OnChanged(object? sender, double e) => UpdateVolume();

    private void UpdateVolume()
    {
        var musicVolume = (float)(m_config.Audio.MusicVolume * m_config.Audio.Volume);
        var soundVolume = m_config.Audio.SoundVolume * m_config.Audio.Volume;

        m_audioSystem.Music.SetVolume(musicVolume);
        m_audioSystem.SetVolume(soundVolume);
    }

    private void SoundFont_OnChanged(object? sender, string _)
    {
        (m_audioSystem.Music as MusicPlayer)?.ChangeSoundFont();
    }

    private void UseOPLEmulation_OnChanged(object? sender, Synth e)
    {
        (m_audioSystem.Music as MusicPlayer)?.SetSynthesizer();
    }
}

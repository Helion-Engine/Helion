using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World.Entities.Players;
using OpenTK.Windowing.Common;
using System;

namespace Helion.Client;

public partial class Client
{
    private void RegisterConfigChanges()
    {
        m_config.Audio.MusicVolume.OnChanged += MusicVolume_OnChanged;
        m_config.Audio.ZMusicVolume.OnChanged += ZMusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged += SoundVolume_OnChanged;

        m_config.Mouse.Look.OnChanged += Look_OnChanged;

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Dimension.OnChanged += WindowDimension_OnChanged;
        m_config.Window.Border.OnChanged += WindowBorder_OnChanged;
        m_config.Window.Display.OnChanged += WindowDisplay_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged += WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged += WindowVirtualDimension_OnChanged;

        m_config.Hud.AutoScale.OnChanged += AutoScale_OnChanged;

        m_config.Compatibility.SessionCompatLevel.OnChanged += SessionCompatLevel_OnChanged;

        m_config.Controller.EnableGameController.OnChanged += EnableGameController_OnChanged;
        m_config.Controller.GameControllerDeadZone.OnChanged += GameControllerDeadZone_OnChanged;
        m_config.Controller.EnableRumble.OnChanged += GameControllerRumble_OnChanged;
        m_config.Controller.GyroNoise.OnChanged += GameControllerNoise_OnChanged;
        m_config.Controller.GyroDrift.OnChanged += GameControllerDrift_OnChanged;

        m_config.Developer.LogGC.OnChanged += LogGC_OnChanged;

        CalculateHudScale();
    }

    private void LogGC_OnChanged(object? sender, bool e)
    {
        SetLogGC(e);
    }

    private void SetLogGC(bool e)
    {
        if (e)
        {
            GCNotifier.GarbageCollected += GCNotifier_GarbageCollected;
            GCNotifier.Start();
        }
        else
        {
            GCNotifier.GarbageCollected -= GCNotifier_GarbageCollected;
        }
    }

    private void GCNotifier_GarbageCollected()
    {
        if (!m_config.Developer.LogGC)
            return;

        var eventMessage = $"GC Event {m_stopwatch.Elapsed}";
        var world = m_layerManager.WorldLayer?.World;
        if (world != null)
            world.DisplayMessage(world.Player, null, eventMessage, true);
        else
            Log.Info(eventMessage);
    }

    private void CalculateHudScale()
    {
        if (!m_config.Hud.AutoScale)
            return;

        int ratio = Math.Clamp((int)Math.Ceiling(m_window.ClientSize.Y / 360.0), 1, 10);
        m_config.Hud.Scale.Set(ratio);
    }

    private void AutoScale_OnChanged(object? sender, bool set)
    {
        if (set)
            CalculateHudScale();
    }

    private void WindowDisplay_OnChanged(object? sender, int display)
    {
        m_window.SetDisplay(display);
        m_window.UpdateWindow();
        CalculateHudScale();
    }

    private void WindowVirtualEnable_OnChanged(object? sender, bool set) =>
        CalculateHudScale();

    private void WindowVirtualDimension_OnChanged(object? sender, Dimension dim) =>
        CalculateHudScale();

    private void WindowBorder_OnChanged(object? sender, WindowBorder border)
    {
        m_window.UpdateWindow();
    }

    private void WindowDimension_OnChanged(object? sender, Dimension dimension)
    {
        m_window.UpdateWindow();
        CalculateHudScale();
    }

    private void WindowState_OnChanged(object? sender, RenderWindowState state)
    {
        m_window.UpdateWindow();
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
        m_config.Audio.ZMusicVolume.OnChanged -= ZMusicVolume_OnChanged;
        m_config.Audio.SoundVolume.OnChanged -= SoundVolume_OnChanged;

        m_config.Mouse.Look.OnChanged -= Look_OnChanged;

        m_config.Window.State.OnChanged -= WindowState_OnChanged;
        m_config.Window.Dimension.OnChanged -= WindowDimension_OnChanged;
        m_config.Window.Border.OnChanged -= WindowBorder_OnChanged;
        m_config.Window.Display.OnChanged -= WindowDisplay_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged -= WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged -= WindowVirtualDimension_OnChanged;

        m_config.Hud.AutoScale.OnChanged -= AutoScale_OnChanged;

        m_config.Compatibility.SessionCompatLevel.OnChanged -= SessionCompatLevel_OnChanged;

        m_config.Controller.EnableGameController.OnChanged -= EnableGameController_OnChanged;
        m_config.Controller.GameControllerDeadZone.OnChanged -= GameControllerDeadZone_OnChanged;
        m_config.Controller.EnableRumble.OnChanged -= GameControllerRumble_OnChanged;
        m_config.Controller.GyroNoise.OnChanged -= GameControllerNoise_OnChanged;
        m_config.Controller.GyroDrift.OnChanged -= GameControllerDrift_OnChanged;
    }

    private void SoundVolume_OnChanged(object? sender, double volume) => UpdateVolume();

    private void MusicVolume_OnChanged(object? sender, double volume) => UpdateVolume();

    private void ZMusicVolume_OnChanged(object? sender, double volume) => UpdateVolume();

    private void UpdateVolume()
    {
        m_audioSystem.SetVolume(m_config.Audio.SoundVolume);
        m_audioSystem.Music.SetVolume();
    }

    private void SessionCompatLevel_OnChanged(object? sender, Resources.Definitions.CompLevel e)
    {
        m_archiveCollection.Definitions.CompLevelDefinition.CompLevel = e;
        m_archiveCollection.Definitions.CompLevelDefinition.Apply(m_config, true);
    }

    private void GameControllerDeadZone_OnChanged(object? sender, double e)
    {
        m_window.JoystickAdapter.AnalogDeadZone = (float)e;
    }

    private void EnableGameController_OnChanged(object? sender, bool e)
    {
        m_window.JoystickAdapter.Enabled = e;
    }
    private void GameControllerRumble_OnChanged(object? sender, bool e)
    {
        m_window.JoystickAdapter.RumbleEnabled = e;
    }

    private void GameControllerNoise_OnChanged(object? sender, Vec3F e)
    {
        m_window.JoystickAdapter.SetGyroCalibration(m_config.Controller.GyroNoise, m_config.Controller.GyroDrift);
    }
    private void GameControllerDrift_OnChanged(object? sender, Vec3F e)
    {
        m_window.JoystickAdapter.SetGyroCalibration(m_config.Controller.GyroNoise, m_config.Controller.GyroDrift);
    }
}

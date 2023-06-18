using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using System;

namespace Helion.Client;

public partial class Client
{
    [ConsoleCommand("mouselook", "Toggle mouselook")]
    private void ToggleMouselook(ConsoleCommandEventArgs args)
    {
        m_config.Mouse.Look.Set(!m_config.Mouse.Look.Value);
    }

    [ConsoleCommand("autoaim", "Toggle auto aim")]
    private void ToggleAutoaim(ConsoleCommandEventArgs args)
    {
        m_config.Game.AutoAim.Set(!m_config.Game.AutoAim.Value);
    }


    [ConsoleCommand("screenshot", "Capture a screenshot")]
    private void Screenshot(ConsoleCommandEventArgs args)
    {
        m_takeScreenshot = true;
    }

    [ConsoleCommand("chasecam", "Toggles chase camera mode")]
    private void ToggleChaseCam(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.ToggleChaseCameraMode();
    }
}

using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Util.Loggers;
using Helion.Util;

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

    [ConsoleCommand("markspecials", "Toggles mark specials")]
    private void ToggleMarkSpecials(ConsoleCommandEventArgs args)
    {
        m_config.Game.MarkSpecials.Set(!m_config.Game.MarkSpecials.Value);
    }

    [ConsoleCommand("marksecrets", "Toggles mark secrets")]
    private void ToggleMarkSecrets(ConsoleCommandEventArgs args)
    {
        m_config.Game.MarkSecrets.Set(!m_config.Game.MarkSecrets.Value);
    }

    [ConsoleCommand(Constants.Input.GammaCorrection, "Cycles the gamma correction value")]
    private void GammaCorrection(ConsoleCommandEventArgs args)
    {
        var value = m_config.Render.GammaCorrection.Value;
        m_config.Render.GammaCorrection.Set(value + 0.1);
        if (value == m_config.Render.GammaCorrection)
            m_config.Render.GammaCorrection.Set(1);
        HelionLog.Info($"Gamma correction level {value:F1}");
    }
}

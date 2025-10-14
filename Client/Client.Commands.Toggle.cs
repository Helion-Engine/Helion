using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Util.Loggers;
using Helion.Util;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Client;

public partial class Client
{
    /// <summary>
    /// Cycles a config setting's value through the provided values (optional if boolean).
    /// If the current value is not in the list, the first one is used.
    /// </summary>
    [ConsoleCommand("toggle", "Toggles a config setting")]
    private void Toggle(ConsoleCommandEventArgs args)
    {
        string? configKey = args.Args.FirstOrDefault();
        if (configKey == null)
        {
            HelionLog.Error("Config setting not provided");
            return;
        }
        if (!m_config.TryGetComponent(configKey, out ConfigComponent? component))
        {
            HelionLog.Error($"Config setting {configKey} not found");
            return;
        }

        // if boolean, allow toggling without providing values
        if (args.Args.Count == 1)
        {
            if (component.Value is ConfigValue<bool> boolConfigVal)
                TryHandleConfigVariableCommand(new ConsoleCommandEventArgs($"{configKey} {!boolConfigVal.Value}"));
            else
                HelionLog.Error($"Must provide values for {configKey}, since it is not a True/False config setting");
        }
        else
        {
            List<object> parsedValues = [];
            foreach (string arg in args.Args[1..])
            {
                if (!component.Value.TryConvert(arg, out object? parsedVal) || parsedVal == null)
                {
                    HelionLog.Error($"Invalid value '{arg}' provided for {configKey}");
                    return;
                }
                parsedValues.Add(parsedVal);
            }
            int nextIndex = (parsedValues.IndexOf(component.Value.ObjectValue) + 1) % parsedValues.Count;
            TryHandleConfigVariableCommand(new ConsoleCommandEventArgs($"{configKey} {parsedValues[nextIndex]}"));
        }
    }

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
        bool newValue = !m_config.Game.MarkSpecials.Value;
        m_config.Game.MarkSpecials.Set(newValue);
        HelionLog.Info($"Special marking {(newValue ? "on" : "off")}");
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
        HelionLog.Info($"Gamma correction level {m_config.Render.GammaCorrection:F1}");
    }
}

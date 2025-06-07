using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Helion.Layer.IwadSelection;

public readonly struct IwadSelection(string iwad, string pwad)
{
    public readonly string IWad = iwad;
    public readonly string PWad = pwad;
}

public class IwadSelectionLayer : IGameLayer
{
    private struct IwadData(string fullPath, string name, IWadInfo iWadInfo)
    {
        public string FullPath = fullPath;
        public string Name = name;
        public IWadInfo IWadInfo = iWadInfo;
    }

    public event EventHandler<IwadSelection>? OnIwadSelected;

    private static readonly string ConsoleFont = Constants.Fonts.Console;
    private readonly IConfig m_config;
    private readonly List<IwadData> m_iwadData = [];
    private readonly Stopwatch m_stopwatch = new();
    private int m_selectedIndex;
    private bool m_indicator;
    private bool m_loading;

    public IwadSelectionLayer(IConfig config, IList<IWadPath> iwadData)
    {
        m_config = config;

        var hasDoom2 = iwadData.Any(x => x.Info.IWadType == IWadType.Doom2 || x.Info.IWadType == IWadType.FreeDoom2);
        foreach (var data in iwadData)
        {
            if (data.Info.IsPWadAddOn() && !hasDoom2)
                continue;

            m_iwadData.Add(new(data.Path, $"{Path.GetFileName(data.Path)}: {data.Info.Title}", data.Info));
        }

        m_stopwatch.Start();
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        int fontSize = m_config.Window.GetMenuLargeFontSize();
        int spacer = m_config.Window.GetMenuScaled(8);

        hud.RenderFullscreenImageStretched(Constants.DefaultBackgroundImage);

        Dimension dim;
        int y = -((fontSize + spacer) * m_iwadData.Count) / 2;

        if (m_iwadData.Count == 0)
        {
            y += spacer * 3;
            hud.Text($"No IWADs found :(", ConsoleFont, fontSize, (0, y), out dim, both: Align.Center);
            y += dim.Height + spacer;
            hud.Text($"Copy DOOM2.WAD to the Helion directory or launch with -iwad", ConsoleFont, fontSize, (0, y), out dim, both: Align.Center);
            y += dim.Height + spacer;
            hud.Text("Press any key to exit", ConsoleFont, fontSize, (0, y), both: Align.Center);
            return;
        }

        hud.Text("Select which IWAD to run:", ConsoleFont, fontSize, (0, y), out dim, both: Align.Center);
        y += dim.Height + spacer;
        int maxWidth = 0;
        int selectedY = 0;

        foreach (var data in m_iwadData)
        {
            var measuredDim = hud.MeasureText(data.Name, ConsoleFont, fontSize);
            if (measuredDim.Width > maxWidth)
                maxWidth = measuredDim.Width;
        }

        for (int i = 0; i < m_iwadData.Count; i++)
        {
            var data = m_iwadData[i];
            var text = data.Name;
            var currentDim = hud.MeasureText(text, ConsoleFont, fontSize);
            hud.Text(text, ConsoleFont, fontSize, (-((maxWidth - currentDim.Width) / 2), y), out dim, both: Align.Center);
            if (i == m_selectedIndex)
                selectedY = y;
            y += dim.Height + spacer;
        }

        if (m_stopwatch.ElapsedMilliseconds >= 200)
        {
            m_indicator = !m_indicator;
            m_stopwatch.Restart();
        }

        hud.Image("arrow-right", (-maxWidth / 2 - (fontSize / 2) - spacer, selectedY), both: Align.Center, scale: fontSize / 100.0f,
            alpha: m_indicator ? 1.0f : 0.5f);
    }

    public void HandleInput(IConsumableInput input)
    {
        if (m_loading)
            return;

        if (m_iwadData.Count == 0 && input.HasAnyKeyPressed())
        {
            Environment.Exit(0);
            return;
        }

        if (input.ConsumeKeyPressed(Key.Enter) && m_selectedIndex < m_iwadData.Count)
        {
            m_loading = true;

            var selection = m_iwadData[m_selectedIndex];
            string iwad = selection.FullPath;
            var pwad = string.Empty;
            // No Rest for the Living is a special case where it's a PWAD requiring Doom2 IWAD
            if (selection.IWadInfo.IsPWadAddOn())
            {
                pwad = iwad;
                iwad = GetIWadForPWadAddOn();
            }

            OnIwadSelected?.Invoke(this, new(iwad, pwad));
        }

        if (input.ConsumePressOrContinuousHold(Key.Down))
            m_selectedIndex = ++m_selectedIndex % m_iwadData.Count;
        if (input.ConsumePressOrContinuousHold(Key.Up))
            --m_selectedIndex;

        if (m_selectedIndex < 0)
            m_selectedIndex = m_iwadData.Count + m_selectedIndex;
    }

    private string GetIWadForPWadAddOn()
    {
        var doom2 = m_iwadData.Where(x => x.IWadInfo.IWadType == IWadType.Doom2).Cast<IwadData?>().FirstOrDefault();
        if (doom2 != null)
            return doom2.Value.FullPath;

        var freedoom = m_iwadData.Where(x => x.IWadInfo.IWadType == IWadType.FreeDoom2).Cast<IwadData?>().FirstOrDefault();
        if (freedoom != null)
            return freedoom.Value.FullPath;
        return string.Empty;
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        
    }

    public void Dispose()
    {
        
    }
}

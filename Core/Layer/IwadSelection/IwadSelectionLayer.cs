using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Resources.Archives.Collection;
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

public class IwadSelectionLayer : IGameLayer
{
    private struct IwadData
    {
        public string FullPath;
        public string Name;
        public IWadInfo IWadInfo;

        public IwadData(string fullPath, string name, IWadInfo iWadInfo)
        {
            FullPath = fullPath;
            Name = name;
            IWadInfo = iWadInfo;
        }
    }

    public event EventHandler<string>? OnIwadSelected;

    private static readonly string ConsoleFont = Constants.Fonts.Console;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly IConfig m_config;
    private readonly List<IwadData> m_iwadData = new();
    private readonly Stopwatch m_stopwatch = new();
    private int m_selectedIndex;
    private IwadData? m_loading;
    private bool m_indicator;

    public IwadSelectionLayer(ArchiveCollection archiveCollection, IConfig config)
    {
        m_archiveCollection = archiveCollection;
        m_config = config;
        IWadLocator iwadLocator = new(new[] { Directory.GetCurrentDirectory() });
        var iwadData = iwadLocator.Locate().OrderBy(x => Path.GetFileName(x.Item1));
        foreach (var data in iwadData)
            m_iwadData.Add(new(data.Item1, $"{Path.GetFileName(data.Item1)}: {data.Item2.Title}", data.Item2));

        m_stopwatch.Start();
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        int fontSize = m_config.Hud.GetScaled(20);
        int spacer = m_config.Hud.GetScaled(8);

        hud.RenderFullscreenImage("background");
        hud.FillBox(new(new Vec2I(0, 0), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)), Color.Black, alpha: 0.8f);

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

        if (m_loading != null)
            hud.Text($"Loading {m_loading.Value.Name}...", ConsoleFont, fontSize, (0, y + (spacer * 3)), out dim, both: Align.Center);
    }

    public void HandleInput(IConsumableInput input)
    {
        if (m_loading != null)
            return;

        if (m_iwadData.Count == 0 && input.HasAnyKeyPressed())
        {
            Environment.Exit(0);
            return;
        }

        if (input.ConsumeKeyPressed(Key.Enter) && m_selectedIndex < m_iwadData.Count)
        {
            m_loading = m_iwadData[m_selectedIndex];
            OnIwadSelected?.Invoke(this, m_loading.Value.FullPath);
        }

        if (input.ConsumePressOrContinuousHold(Key.Down))
            m_selectedIndex = ++m_selectedIndex % m_iwadData.Count;
        if (input.ConsumePressOrContinuousHold(Key.Up))
            --m_selectedIndex;

        if (m_selectedIndex < 0)
            m_selectedIndex = m_iwadData.Count + m_selectedIndex;
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        
    }

    public void Dispose()
    {
        
    }

    private Font GetFontOrDefault(string name)
    {
        var font = m_archiveCollection.GetFont(name);
        if (font == null)
            return new Font("Empty", new(), new((0, 0), Graphics.ImageType.Argb));
        return font;
    }
}

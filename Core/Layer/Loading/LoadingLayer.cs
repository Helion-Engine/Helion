using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Fonts;
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
using System.Diagnostics;

namespace Helion.Layer.IwadSelection;

public class LoadingLayer : IGameLayer
{
    private static readonly string ConsoleFont = Constants.Fonts.Console;
    private static readonly string[] Spinner = new[] { "-", "\\", "|", "/" };

    private readonly ArchiveCollection m_archiveCollection;
    private readonly IConfig m_config;
    private readonly Stopwatch m_stopwatch = new();
    private int m_spinner;
    public string LoadingText { get; set; }
    public string LoadingImage { get; set; } = string.Empty;

    public LoadingLayer(ArchiveCollection archiveCollection, IConfig config, string text)
    {
        m_archiveCollection = archiveCollection;
        m_config = config;
        LoadingText = text;
        m_stopwatch.Start();
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        if (LoadingImage.Length > 0)
            hud.RenderFullscreenImage(LoadingImage);

        int fontSize = m_config.Hud.GetScaled(20);
        int yOffset = -m_config.Hud.GetScaled(8);
        var dim = hud.MeasureText(LoadingText, ConsoleFont, fontSize);
        hud.FillBox(new(new Vec2I(0, hud.Dimension.Height  - dim.Height + (yOffset*2)), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)), Color.Black, alpha: 0.7f);
        hud.Text(LoadingText, ConsoleFont, fontSize, (0, yOffset), both: Align.BottomMiddle);

        if (m_stopwatch.ElapsedMilliseconds >= 80)
        {
            m_spinner = ++m_spinner % Spinner.Length;
            m_stopwatch.Restart();
        }
                
        hud.Text(Spinner[m_spinner], ConsoleFont, fontSize, (-dim.Width / 2 - m_config.Hud.GetScaled(16), yOffset), both: Align.BottomMiddle);
    }

    public void HandleInput(IConsumableInput input)
    {

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

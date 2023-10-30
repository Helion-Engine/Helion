using Helion.Geometry;
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

namespace Helion.Layer.IwadSelection;

public class LoadingLayer : IGameLayer
{
    private static readonly string ConsoleFont = Constants.Fonts.Console;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly IConfig m_config;
    public string LoadingText { get; set; }
    public string LoadingImage { get; set; } = string.Empty;

    public LoadingLayer(ArchiveCollection archiveCollection, IConfig config, string text)
    {
        m_archiveCollection = archiveCollection;
        m_config = config;
        LoadingText = text;
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        if (LoadingImage.Length > 0)
            hud.RenderFullscreenImage(LoadingImage);

        hud.Text(LoadingText, ConsoleFont, m_config.Hud.GetScaled(20), (0, -m_config.Hud.GetScaled(8)), both: Align.BottomMiddle);
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

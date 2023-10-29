using Helion.Geometry;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using System;

namespace Helion.Layer.IwadSelection;

public class LoadingLayer : IGameLayer
{
    private static readonly string ConsoleFont = "Console";
    private readonly ArchiveCollection m_archiveCollection;
    public string LoadingText { get; set; }

    public LoadingLayer(ArchiveCollection archiveCollection, string text)
    {
        m_archiveCollection = archiveCollection;
        LoadingText = text;
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        hud.Text(LoadingText, ConsoleFont, 24, (0, -8), both: Align.BottomMiddle);
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

using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Timing;

namespace Helion.Layer.EndGame;

public partial class EndGameLayer
{
    private const string FontName = Constants.Fonts.Small;
    private static readonly Vec2I TextStartCorner = new(10, 10);

    private IList<string> m_images = Array.Empty<string>();
    private Vec2I m_theEndOffset = Vec2I.Zero;
    private bool m_initRenderPages;
    private uint m_charsToDraw;

    private readonly record struct HudBackgroundImage(IRenderableSurfaceContext Ctx, IHudRenderContext Hud);
    private readonly record struct HudVirtualText(IList<string> displayText, Ticker ticker, bool showAllText, IHudRenderContext hud);
    private readonly record struct HudVirtualBackgroundImage(string Image, IRenderableTextureHandle FlatHandle, int Width, int Height, IHudRenderContext Hud);

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        hud.Clear(Color.Black);

        if (!m_initRenderPages)
        {
            SetPage(hud);

            if (TheEndImages.Count > 0)
            {
                if (hud.Textures.TryGet(TheEndImages[0], out var handle))
                    m_theEndOffset = new Vec2I(-2 - handle.Dimension.Width / 2, -2 - handle.Dimension.Height / 2);
            }
        }

        if (m_drawState <= EndGameDrawState.TextComplete)
        {
            bool showAllText = m_drawState > EndGameDrawState.Text;
            Draw(m_backgroundImage, m_displayText, m_ticker, showAllText, hud, ctx);
        }
        else if (m_drawState == EndGameDrawState.Cast)
        {
            DrawCast(ctx, hud);
        }
        else
        {
            hud.DoomVirtualResolution(m_virtualDrawBackground, new HudBackgroundImage(ctx, hud));
        }
    }

    private void VirtualDrawBackground(HudBackgroundImage hud)
    {
        DrawBackgroundImages(m_images, m_xOffset, hud.Hud, hud.Ctx);

        if (m_drawState == EndGameDrawState.TheEnd)
            hud.Hud.Image(TheEndImages[m_theEndImageIndex], m_theEndOffset, Align.Center);
    }

    private void DrawCast(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        ctx.ClearDepth();
        hud.Clear(Color.Black);

        hud.RenderFullscreenImage("BOSSBACK");
        hud.DoomVirtualResolution(m_virtualDrawCast, hud);
    }

    private void VirtualDrawCast(IHudRenderContext hud)
    {
        DrawCastMonsterText(hud);

        if (m_castEntity == null)
            return;

        if (hud.Textures is not LegacyGLTextureManager textureManager)
            return;

        var spriteDef = textureManager.GetSpriteDefinition(m_castEntity.Frame.SpriteIndex);
        if (spriteDef == null)
            return;

        var spriteRotation = textureManager.GetSpriteRotation(spriteDef, m_castEntity.Frame.Frame, 0, 0);
        if (spriteRotation == null)
            return;

        string image = spriteRotation.Texture.Name;
        if (!hud.Textures.TryGet(image, out var handle))
            return;

        Vec2I offset = new(160, 170);
        hud.Image(image, offset + RenderDimensions.TranslateDoomOffset(handle.Offset));
    }

    private void DrawCastMonsterText(IHudRenderContext hud)
    {
        int fontSize = hud.GetFontMaxHeight(FontName);
        string text = World.ArchiveCollection.Language.GetMessage(Cast[m_castIndex].DisplayName);
        Dimension size = hud.MeasureText(text, FontName, fontSize);
        Vec2I offset = new(160 - (size.Width / 2), 180);
        hud.Text(text, FontName, fontSize, offset);
    }

    private void SetPage(IHudRenderContext hud)
    {
        m_initRenderPages = true;

        string next = World.MapInfo.Next;
        if (next.EqualsIgnoreCase("EndPic"))
        {
            m_images = new[] { World.MapInfo.EndPic };
        }
        else if (next.EqualsIgnoreCase("EndGame2"))
        {
            m_images = new[] { "VICTORY2" };
        }
        else if (next.EqualsIgnoreCase("EndGame3") || next.EqualsIgnoreCase("EndBunny"))
        {
            m_images = new[] { "PFUB1", "PFUB2" };
            m_shouldScroll = true;
            if (hud.Textures.TryGet(m_images[0], out var handle))
                m_xOffsetStop = handle.Dimension.Width;
        }
        else if (next.EqualsIgnoreCase("EndGame4"))
        {
            m_images = new[] { "ENDPIC" };
        }
        else if (next.EqualsIgnoreCase("EndGameC"))
        {
            m_endGameType = EndGameType.Cast;
        }
        else
        {
            var pages = LayerUtil.GetRenderPages(hud, m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.CreditPages, false);
            if (pages.Count > 0)
                m_images = new[] { pages[^1] };
        }
    }

    private void Draw(string image, IList<string> displayText, Ticker ticker, bool showAllText, IHudRenderContext hud,
        IRenderableSurfaceContext ctx)
    {
        ctx.ClearDepth();
        hud.Clear(Color.Black);
        DrawBackground(image, hud);
        hud.DoomVirtualResolution(m_virtualDrawText, new HudVirtualText(displayText, ticker, showAllText, hud));
    }

    private void VirtualDrawText(HudVirtualText hud)
    {
        DrawText(hud.displayText, hud.ticker, hud.showAllText, hud.hud);
    }

    private void DrawBackgroundImages(IList<string> images, int xOffset, IHudRenderContext hud,
        IRenderableSurfaceContext ctx)
    {
        ctx.ClearDepth();
        hud.Clear(Color.Black);

        if (m_drawState == EndGameDrawState.Complete && images.Count == 1)
        {
            hud.RenderFullscreenImage(images[0]);
            return;
        }
        int imagesRunningWidth = 0;
        for (int i = 0; i < images.Count; i++)
        {
            string image = images[i];
            if (!hud.Textures.TryGet(image, out var handle))
                continue;

            hud.Image(image, (xOffset - imagesRunningWidth, 0));
            imagesRunningWidth += handle.Dimension.Width;
        }
        // When drawing bunny end, cover image spillover to keep viewport consistent
        if (m_shouldScroll && imagesRunningWidth > 0)
        {
            hud.FillBox(new HudBox((-(imagesRunningWidth - hud.Width) + xOffset, 0, 0, hud.Height)), Color.Black, Align.TopLeft);
            hud.FillBox(new HudBox((1, 0, xOffset + 1, hud.Height)), Color.Black, Align.TopRight);
        }
    }

    private void DrawBackground(string image, IHudRenderContext hud)
    {
        if (!hud.Textures.TryGet(image, out var flatHandle, ResourceNamespace.Flats))
            return;

        var (width, height) = flatHandle.Dimension;
        if (width != 64 || height != 64)
        {
            hud.RenderFullscreenImage(image);
            return;
        }

        hud.DoomVirtualResolution(m_virtualDrawBackgroundImage, new HudVirtualBackgroundImage(image, flatHandle, width, height, hud));
    }

    private static void VirtualDrawBackgroundImage(HudVirtualBackgroundImage data)
    {
        int repeatX = data.Hud.Dimension.Width / data.Width;
        int repeatY = data.Hud.Dimension.Height / data.Height;

        if (data.Hud.Dimension.Width % data.Width != 0)
            repeatX++;
        if (data.Hud.Dimension.Height % data.Height != 0)
            repeatY++;

        Vec2I drawCoordinate = (0, 0);
        for (int y = 0; y < repeatY; y++)
        {
            for (int x = 0; x < repeatX; x++)
            {
                data.Hud.Image(data.Image, drawCoordinate);
                drawCoordinate.X += data.Width;
            }

            drawCoordinate.X = 0;
            drawCoordinate.Y += data.Height;
        }
    }

    private void DrawText(IEnumerable<string> lines, Ticker ticker, bool showAllText, IHudRenderContext hud)
    {
        // Default height/spacing is 8/3;
        // Other ports allow taller fonts to eat into that spacing.
        // We'll at least keep 1px
        int fontSize = hud.GetFontMaxHeight(FontName);
        int lineSpacing = Math.Clamp(11 - fontSize, 1, 3);

        // The ticker goes slower than normal, so as long as we see one
        // or more ticks happening then advance the number of characters
        // to draw.
        m_charsToDraw += (uint)ticker.GetTickerInfo().Ticks;

        int charsDrawn = 0;
        int x = TextStartCorner.X;
        int y = TextStartCorner.Y;

        // TODO: This is going to be a pain in the ass to the GC...
        foreach (string line in lines)
        {
            foreach (char c in line)
            {
                if (!showAllText && charsDrawn >= m_charsToDraw)
                    return;

                hud.Text(c.ToString(), FontName, fontSize, (x, y), out Dimension drawArea);
                x += drawArea.Width;
                charsDrawn++;
            }

            x = TextStartCorner.X;
            y += fontSize + lineSpacing;
        }
    }
}

using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Menus.Impl;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Strings;
using Helion.Util;
using Helion.Util.Extensions;
using System;
using System.Collections.Generic;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Menus;

public partial class MenuLayer
{
    private const int ActiveMillis = 500;
    private const int SelectedOffsetX = -32;
    private const int SelectedOffsetY = 5;

    private readonly static List<StringSlice> m_lineWrapLines = [];

    private IMenuComponent? m_previousSelectedComponent;
    private IRenderableTextureHandle? m_saveGameTexture;
    private SaveGameSummary? m_saveGameSummary;

    private bool ShouldDrawActive => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;

    public void Render(IHudRenderContext hud)
    {
        Animation.Tick();
        hud.FillBox((0, 0, hud.Width, hud.Height), Color.Black, alpha: 0.5f);
        hud.DoomVirtualResolution(m_renderVirtualHudAction, hud);
    }

    private void RenderVirtualHud(IHudRenderContext hud)
    {
        if (!m_menus.TryPeek(out Menu? menu))
            return;

        var saveMenu = menu.CurrentComponent is MenuSaveRowComponent;
        var offsetY = menu.TopPixelPadding;
        var detailsEnabled = m_config.Game.ExtendedSaveGameInfo;
        var firstRow = true;

        if (saveMenu)
            DrawSaveMenuBox(hud, detailsEnabled);

        for (int i = 0; i < menu.Components.Count; i++)
        {
            var component = menu.Components[i];
            bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
            bool wasSelected = ReferenceEquals(menu.CurrentComponent, m_previousSelectedComponent);

            switch (component)
            {
                case MenuImageComponent imageComponent:
                    DrawImage(hud, imageComponent, isSelected, ref offsetY, imageComponent.UpscaleWithText ? m_config.Hud.FontUpscalingFactor : 1);
                    break;
                case MenuPaddingComponent paddingComponent:
                    offsetY += paddingComponent.PixelAmount;
                    break;
                case MenuSmallTextComponent smallTextComponent:
                    DrawText(hud, smallTextComponent, ref offsetY);
                    break;
                case MenuLargeTextComponent largeTextComponent:
                    DrawText(hud, largeTextComponent, ref offsetY);
                    break;
                case MenuSaveRowComponent saveRowComponent:
                    if (firstRow)
                    {
                        offsetY++;
                        firstRow = false;
                    }
                    hud.PushOffset(GetSaveMenuOffset(hud));
                    DrawSaveRow(hud, (SaveMenu)menu, saveRowComponent, isSelected, wasSelected, ref offsetY, detailsEnabled);
                    break;
                default:
                    throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
            }

            if (isSelected)
                m_previousSelectedComponent = menu.CurrentComponent;
        }

        if (saveMenu && m_saveGameSummary != null)
            RenderSaveGameDetails(hud);
    }

    private static void DrawText(IHudRenderContext hud, MenuTextComponent text, ref int offsetY)
    {
        var align = text.Align ?? Align.TopMiddle;
        if (align != Align.TopMiddle)
            offsetY = 0;

        int addHeight;
        if (text.LineWrap)
        {
            int rowHeight = 0;
            var height = hud.MeasureText("0", text.FontName, text.Size).Height;
            hud.LineWrap(text.Text, text.FontName, text.Size, hud.Dimension.Width, m_lineWrapLines, out addHeight);

            foreach (var line in m_lineWrapLines)
            {
                hud.Text(line.AsSpan(), text.FontName, text.Size, (0, offsetY + rowHeight), out _, both: align);
                rowHeight += height;
            }
        }
        else
        {
            hud.Text(text.Text, text.FontName, text.Size, (0, offsetY), out Dimension area, both: align);
            addHeight = area.Height;
        }

        if (align == Align.TopMiddle)
            offsetY += addHeight;
    }

    private void DrawImage(IHudRenderContext hud, MenuImageComponent image, bool isSelected, ref int offsetY, int upscalingFactor)
    {
        int drawY = image.PaddingTopY + offsetY;
        if (image.AddToOffsetY)
            offsetY += image.PaddingTopY;

        if (hud.Textures.TryGet(image.ImageName, out var handle, upscalingFactor: upscalingFactor))
        {
            Vec2I offset = TranslateDoomOffset(handle.Offset);
            int offsetX = offset.X + image.OffsetX;

            hud.Image(image.ImageName, (offsetX, drawY + offset.Y), out HudBox area, both: image.ImageAlign, upscalingFactor: upscalingFactor);

            if (isSelected)
                DrawSelectedImage(hud, image, drawY, offsetX);

            if (!image.AddToOffsetY)
                return;

            if (image.OverrideY == null)
                offsetY += area.Height + offset.Y + image.PaddingBottomY;
            else
                offsetY += image.OverrideY.Value;
        }
        else if (!string.IsNullOrEmpty(image.Title))
        {
            const int FontSize = 12;
            const int TextOffsetX = 48;
            Dimension textDimensions = hud.MeasureText(image.Title, Constants.Fonts.Small, FontSize);
            hud.Text(image.Title, Constants.Fonts.Small, FontSize, (TextOffsetX, drawY), both: image.ImageAlign);
            offsetY += textDimensions.Height + 2;

            if (isSelected)
                DrawSelectedImage(hud, image, drawY, TextOffsetX);
        }
    }

    private void DrawSelectedImage(IHudRenderContext hud, MenuImageComponent image, int drawY, int offsetX)
    {
        string selectedName = (ShouldDrawActive ? image.ActiveImage : image.InactiveImage) ?? "";
        if (!hud.Textures.TryGet(selectedName, out var selectedHandle))
            return;

        offsetX += SelectedOffsetX;
        Vec2I selectedOffset = TranslateDoomOffset(selectedHandle.Offset);
        Vec2I drawPosition = selectedOffset + (offsetX, drawY - SelectedOffsetY);
        hud.Image(selectedName, drawPosition, both: image.ImageAlign);
    }

    private static int GetSaveRowWidth(bool detailsEnabled) => detailsEnabled ? 218 : 301;

    private void DrawSaveRow(IHudRenderContext hud, SaveMenu saveMenu, MenuSaveRowComponent saveRowComponent, bool isSelected,
        bool wasPreviouslySelected, ref int offsetY, bool detailsEnabled)
    {
        const string FontName = Constants.Fonts.Small;
        int fontSize = hud.GetFontMaxHeight(FontName) - 2;
        int menuRowWidth = GetSaveRowWidth(detailsEnabled);

        var textDimension = hud.MeasureText("_", FontName, fontSize);
        var textHeight = textDimension.Height; 

        string saveText;
        var textRowWidth = menuRowWidth - 8;
        if (isSelected && saveMenu.IsTypingName)
        {
            var typedTextRowWidth = textRowWidth;
            //Account for cursor flashing
            if (!saveRowComponent.Text.EndsWith('_'))
                typedTextRowWidth -= textDimension.Width;
            saveText = hud.GetTypedText(saveRowComponent.Text, FontName, fontSize, typedTextRowWidth);
        }
        else
        {
            saveText = hud.GetEllipsesText(saveRowComponent.Text, FontName, fontSize, textRowWidth);
        }
        
        var rowHeight = textHeight + 3;
        hud.AddOffset((17, 0));

        if (isSelected)
        {
            hud.PushAlpha(0.5f);
            HudBox box = new((0, offsetY), (textRowWidth, offsetY + rowHeight));
            hud.FillBox(box, Color.Blue);
            hud.PopAlpha();
        }

        hud.Text(saveText, FontName, fontSize, (1, offsetY + 2));
        offsetY += rowHeight;

        if (isSelected && detailsEnabled && !wasPreviouslySelected)
        {
            m_saveGameSummary = saveRowComponent.SaveGame == null
                ? null
                : new SaveGameSummary(saveRowComponent.SaveGame);

            var saveFilter = m_config.Render.Filter.Texture.Value;
            m_config.Render.Filter.Texture.Set(FilterType.Bilinear, false);
            m_saveGameTexture = m_saveGameSummary?.UpdateSaveGameTexture(hud);
            m_config.Render.Filter.Texture.Set(saveFilter, false);
        }

        hud.PopOffset();
    }

    private static void DrawSaveMenuBox(IHudRenderContext hud, bool detailsEnabled)
    {
        int height = 164;
        hud.PushOffset((16, 20));
        hud.AddOffset(GetSaveMenuOffset(hud));
        int saveRowWidth = GetSaveRowWidth(detailsEnabled);
        var box = new HudBox((0, 0), (saveRowWidth - 6, height));
        hud.PushAlpha(0.65f);
        hud.BorderBox(box, Color.DarkGray, 1);
        box = new HudBox((1, 1), (saveRowWidth - 7, height - 1));
        hud.FillBox(box, Color.Black);
        hud.PopAlpha();
        hud.PopOffset();
    }

    private static bool SaveMenuWide(IHudRenderContext hud) =>
        hud.WindowDimension.Width > 320 && hud.WindowDimension.AspectRatio > 1.45f;

    private static Vec2I GetSaveMenuOffset(IHudRenderContext hud)
    {
        if (SaveMenuWide(hud))
            return (-28, 0);
        return Vec2I.Zero;
    }

    private void RenderSaveGameDetails(IHudRenderContext hud)
    {
        if (m_saveGameSummary == null)
            return;

        const string Font = Constants.Fonts.Small;
        const float ImageAspect = 4 / 3f;

        bool wideScreen = SaveMenuWide(hud);
        int textSize = wideScreen ? 6 : 4;
        int boxWidth = wideScreen ? 128 : 80;
        int thumbnailHeight = (int)(boxWidth / ImageAspect * 0.8f);
        int boxHeight = thumbnailHeight + 5 * textSize + 3;

        var centerOffset = GetSaveMenuOffset(hud);
        hud.LineWrap(m_saveGameSummary.MapName, Font, textSize, boxWidth - 4, m_lineWrapLines, 
            out var requiredHeight);
        boxHeight += requiredHeight;

        Vec2I boxUpperLeftBorder = (229, 20) + centerOffset;
        Vec2I boxLowerRightBorder = boxUpperLeftBorder + (boxWidth + 2, boxHeight + 2);

        Vec2I boxUpperLeft = (230, 21) + centerOffset;
        Vec2I boxLowerRight = boxUpperLeft + (boxWidth, boxHeight);

        hud.PushAlpha(0.65f);
        hud.BorderBox(new HudBox(boxUpperLeftBorder, boxLowerRightBorder), Color.DarkGray, 1);
        hud.FillBox((boxUpperLeft, boxLowerRight), Color.Black);
        hud.PopAlpha();

        if (m_saveGameTexture == null)
        {
            hud.PushOffset(boxUpperLeft);
            var size = hud.MeasureText("No Image", Font, textSize);
            hud.Text("No Image", Font, textSize, (boxWidth / 2 - size.Width / 2, thumbnailHeight / 2 - size.Height / 2), textAlign: TextAlign.Center);
            hud.PopOffset();
        }
        else
        {
            var imageBox = new HudBox(boxUpperLeft, boxUpperLeft + (boxWidth, thumbnailHeight));
            hud.Image(SaveGameSummary.TEXTURENAME, imageBox);
        }

        Vec2I offset = boxUpperLeft + (2, thumbnailHeight + 2);

        for (int i = 0; i < m_lineWrapLines.Count; i++)
        {
            hud.Text(m_lineWrapLines[i].AsSpan(), Font, textSize, offset, out var drawArea);
            offset += (0, drawArea.Height);
        }

        hud.Text(m_saveGameSummary.Date, Font, textSize, offset, out var area);
        offset += (0, area.Height);
        offset += (0, area.Height);

        foreach (string str in m_saveGameSummary?.Stats ?? [])
        {
            hud.Text(str, Constants.Fonts.Small, textSize, offset, out area);
            offset += (0, area.Height);
        }
    }
}

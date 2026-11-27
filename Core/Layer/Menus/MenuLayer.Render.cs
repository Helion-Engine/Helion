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
using Helion.Render.OpenGL.Commands;
using Helion.Strings;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Util;
using System;
using System.Collections.Generic;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Menus;

public partial class MenuLayer
{
    const string StaticMenuImageName = "HELION_STATIC_MENU_IMAGE";
    const int StaticMenuWidth = 200;

    private bool m_resetMouse = true;
    private bool m_initMouse = true;
    private bool m_forceCheckMouse;
    private const int ActiveMillis = 500;
    private const int SelectedOffsetX = -32;
    private const int SelectedOffsetY = 5;

    private readonly static List<StringSlice> m_lineWrapLines = [];
    private readonly static Image StaticMenuImage = new((StaticMenuWidth, MainMenu.MenuItemHeight), ImageType.Argb);

    record struct TextArea(Box2I Area, Vec2I Offset, string Text, bool Center);

    private readonly DynamicArray<TextArea> m_textAreas = new(32);

    private IMenuComponent? m_previousSelectedComponent;
    private IRenderableTextureHandle? m_saveGameTexture;
    private SaveGameSummary? m_saveGameSummary;

    private bool ShouldDrawActive => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;

    public void Render(IHudRenderContext hud)
    {
        if (!hud.Textures.TryGet(StaticMenuImageName, out _))
            hud.Textures.CreateOrReplaceTexture(StaticMenuImageName, Resources.ResourceNamespace.Graphics, StaticMenuImage);

        Animation.Tick();
        hud.FillBox((0, 0, hud.Width, hud.Height), Color.Black, alpha: 0.5f);
        hud.DoomVirtualResolution(m_renderVirtualHudAction, hud);
        RenderNativeResolution(hud);
        if (!m_window.GrabCursor)
            m_mouseMenu.Render(hud);
    }

    private void RenderNativeResolution(IHudRenderContext hud)
    {
        return;
        var resolutionInfo = new ResolutionInfo((320, 240), ResolutionScale.Center, 320/240f);
        var scale = RenderCommands.GetVirtualScale(hud.Dimension, resolutionInfo, out var centerOffsetX);

        for (int i = 0; i < m_textAreas.Length; i++)
        {
            var textArea = m_textAreas.Data[i];
            var pos = textArea.Offset + textArea.Area.Min;
            pos.Y += 1;
            var textAreaScale = new Box2D(textArea.Area.Min.Double * scale, textArea.Area.Max.Double * scale);
            var posScale = pos.Double * scale;

            if (textArea.Center)
                posScale.X += centerOffsetX;

            hud.Text(textArea.Text, Constants.Fonts.Options, (int)textAreaScale.Height, posScale.Int, out var dim);
        }
    }

    private void RenderVirtualHud(IHudRenderContext hud)
    {
        m_textAreas.Clear();
        if (!m_menus.TryPeek(out Menu? menu))
            return;

        m_mouseMenu.Clear();
        m_mouseMenu.SetLocked(menu.RowLocked);

        var saveMenu = menu.CurrentComponent is MenuSaveRowComponent;
        var offsetY = menu.TopPixelPadding;
        var detailsEnabled = m_config.Game.ExtendedSaveGameInfo;
        var firstRow = true;
        var isLegacyMenu = menu is MainMenu || menu is NewGameEpisodeMenu;

        var scaleWidth = m_window.ClientDimension.Width / (float)hud.Dimension.Width;
        var scaleHeight = m_window.ClientDimension.Height / (float)hud.Dimension.Height;

        if (saveMenu)
        {
            hud.PushVirtualDimension((320, 240));
            DrawSaveMenuBox(hud, detailsEnabled);
        }

        for (int i = 0; i < menu.Components.Count; i++)
        {
            var component = menu.Components[i];
            bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
            bool wasSelected = ReferenceEquals(menu.CurrentComponent, m_previousSelectedComponent);

            Box2I drawArea = default;
            switch (component)
            {
                case MenuImageComponent imageComponent:
                    DrawImage(hud, imageComponent, isSelected, ref offsetY, imageComponent.UpscaleWithText ? m_config.Hud.FontUpscalingFactor : 1, isLegacyMenu, out drawArea);
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
                    var hudOffset = GetSaveMenuOffset(hud);
                    hud.PushOffset(hudOffset);
                    var textArea = DrawSaveRow(hud, (SaveMenu)menu, saveRowComponent, isSelected, wasSelected, ref offsetY, detailsEnabled, out drawArea);
                    textArea.Offset += new Vec2I(-hudOffset.X, -hudOffset.Y);
                    m_textAreas.Add(textArea);
                    break;
                default:
                    throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
            }

            if (component.HasAction && drawArea.Max.X != 0)
            {
                var scaleDrawArea = new Box2I(((int)(drawArea.Min.X * scaleWidth), (int)(drawArea.Min.Y * scaleHeight)), 
                    ((int)(drawArea.Max.X * scaleWidth), (int)(drawArea.Max.Y * scaleHeight)));
                m_mouseMenu.Add(scaleDrawArea, i);
            }

            if (isSelected)
                m_previousSelectedComponent = menu.CurrentComponent;
        }

        if (saveMenu && m_saveGameSummary != null)
            RenderSaveGameDetails(hud);

        if (m_resetMouse)
        {
            m_window.SetGrabCursor(false);
            m_resetMouse = false;
        }

        if (m_initMouse)
        {
            m_initMouse = false;
            m_mouseMenu.SetMousePosition((m_window.ClientDimension.Width / 2, (int)(16 * scaleHeight)));
        }

        if ((m_forceCheckMouse || m_mouseMenu.MousePositionChanged()) && m_mouseMenu.GetSelectedIndex(out var selectedIndex))
        {
            menu.SetComponentIndex(selectedIndex);
            m_forceCheckMouse = false;
        }

        if (saveMenu)
        {
            hud.PopVirtualDimension();
        }
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

    private void DrawImage(IHudRenderContext hud, MenuImageComponent image, bool isSelected, ref int offsetY, int upscalingFactor, bool isLegacyMenu, out Box2I drawArea)
    {
        drawArea = default;
        int drawY = image.PaddingTopY + offsetY;
        if (image.AddToOffsetY)
            offsetY += image.PaddingTopY;

        bool renderTitleText = !hud.Textures.TryGet(image.ImageName,  out var handle, upscalingFactor: upscalingFactor) && !string.IsNullOrEmpty(image.Title);
        if (renderTitleText)
        {
            const int FontSize = 12;
            const int TextOffsetX = 48;
            Dimension textDimensions = hud.MeasureText(image.Title, Constants.Fonts.Small, FontSize);
            hud.Text(image.Title, Constants.Fonts.Small, FontSize, (TextOffsetX, drawY), both: image.ImageAlign);
            offsetY += textDimensions.Height + 2;
            drawArea = new((TextOffsetX, drawY), (TextOffsetX + StaticMenuWidth, drawY + textDimensions.Height));

            if (isSelected)
                DrawSelectedImage(hud, image, drawY, TextOffsetX);
            return;
        }

        var useImageName = image.ImageName;
        if (handle == null)
        {
            useImageName = StaticMenuImageName;
            if (!hud.Textures.TryGet(StaticMenuImageName, out handle))
                return;
        }

        var offset = TranslateDoomOffset(handle.Offset);
        var offsetX = offset.X + image.OffsetX;

        hud.Image(useImageName, (offsetX, drawY + offset.Y), out HudBox area, both: image.ImageAlign, upscalingFactor: upscalingFactor);
        drawArea = new(area.Min, area.Max);

        // Always use static draw area
        if (isLegacyMenu && useImageName != StaticMenuImageName)
        {
            hud.Image(StaticMenuImageName, ((320 - StaticMenuWidth) / 2, drawY + offset.Y), out area, both: Align.TopLeft);
            drawArea = new(area.Min, area.Max);
        }

        if (isSelected)
            DrawSelectedImage(hud, image, drawY, offsetX);

        if (!image.AddToOffsetY)
            return;

        if (image.OverrideY == null)
            offsetY += area.Height + offset.Y + image.PaddingBottomY;
        else
            offsetY += image.OverrideY.Value;        
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

    private TextArea DrawSaveRow(IHudRenderContext hud, SaveMenu saveMenu, MenuSaveRowComponent saveRowComponent, bool isSelected,
        bool wasPreviouslySelected, ref int offsetY, bool detailsEnabled, out Box2I drawArea)
    {
        bool isTypingTarget = isSelected && saveMenu.IsTypingName;

        //string fontName = (saveRowComponent.VerificationResult == SaveVerificationResult.Success || isTypingTarget) ? Constants.Fonts.Small : Constants.Fonts.SmallGray;
        //int fontSize = hud.GetFontMaxHeight(fontName) - 2;
        var fontName = Constants.Fonts.Options;
        var fontSize = 8;
        int menuRowWidth = GetSaveRowWidth(detailsEnabled);

        var textDimension = hud.MeasureText("_", fontName, fontSize);
        var textHeight = textDimension.Height;

        string saveText;
        var textRowWidth = menuRowWidth - 8;
        if (isTypingTarget)
        {
            var typedTextRowWidth = textRowWidth;
            //Account for cursor flashing
            if (!saveRowComponent.Text.EndsWith('_'))
                typedTextRowWidth -= textDimension.Width;
            saveText = hud.GetTypedText(saveRowComponent.Text, fontName, fontSize, typedTextRowWidth);
        }
        else
        {
            saveText = hud.GetEllipsesText(saveRowComponent.Text, fontName, fontSize, textRowWidth);
        }
        
        var rowHeight = textHeight + 3;
        var offset = new Vec2I(17, 0);
        hud.AddOffset(offset);

        HudBox box = new((0, offsetY), (textRowWidth, offsetY + rowHeight));
        drawArea = new(box.Min, box.Max);

        if (isSelected)
        {
            hud.PushAlpha(0.5f);
            hud.FillBox(box, Color.Blue);
            hud.PopAlpha();
        }

        hud.Text(saveText, fontName, fontSize, (1, offsetY + 2));
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
        offset.X += 1;
        return new(new Box2I(new Vec2I(drawArea.Min.X, drawArea.Min.Y), new Vec2I(drawArea.Max.X, drawArea.Max.Y - 1)), offset, saveText, false);
    }

    private static void DrawSaveMenuBox(IHudRenderContext hud, bool detailsEnabled)
    {
        int height = 192;
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
            return (28, 0);
        return Vec2I.Zero;
    }

    private void RenderSaveGameDetails(IHudRenderContext hud)
    {
        if (m_saveGameSummary == null)
            return;

        //string fontName = m_saveGameSummary.VerificationResult == SaveVerificationResult.Success ? Constants.Fonts.Small : Constants.Fonts.SmallGray;
        var fontName = Constants.Fonts.Options;
        const float ImageAspect = 4 / 3f;

        bool wideScreen = SaveMenuWide(hud);
        int textSize = 6;
        int boxWidth = wideScreen ? 128 : 80;
        int thumbnailHeight = (int)(boxWidth / ImageAspect);
        int boxHeight = thumbnailHeight + textSize * (m_saveGameSummary.VerificationResult == SaveVerificationResult.Success ? 5 : 7) + 3;

        var centerOffset = GetSaveMenuOffset(hud);
        var testString = "This is a test. This is a test. This is a test. This is a test. This is a test. This is a test. ";
        hud.LineWrap(testString, fontName, textSize, boxWidth - 4, m_lineWrapLines, 
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
            var size = hud.MeasureText("No Image", fontName, textSize);
            hud.Text("No Image", fontName, textSize, (boxWidth / 2 - size.Width / 2, thumbnailHeight / 2 - size.Height / 2), textAlign: TextAlign.Center);
            m_textAreas.Add(new TextArea(new Box2I(Vec2I.Zero, new Vec2I(0, textSize)), boxUpperLeft + (boxWidth / 2 - size.Width / 2, thumbnailHeight / 2 - size.Height / 2), "No Image", true));
            hud.PopOffset();
        }
        else
        {
            var imageBox = new HudBox(boxUpperLeft, boxUpperLeft + (boxWidth, thumbnailHeight));
            hud.Image(SaveGameSummary.TEXTURENAME, imageBox);
        }

        Vec2I offset = boxUpperLeft + (2, thumbnailHeight);

        for (int i = 0; i < m_lineWrapLines.Count; i++)
        {
            //var drawArea = hud.MeasureText(m_lineWrapLines[i].AsSpan(), fontName, textSize);
            m_textAreas.Add(new TextArea(new Box2I(Vec2I.Zero, new Vec2I(0, textSize)), (offset.X - 54, offset.Y), m_lineWrapLines[i].ToString(), true));
            hud.Text(m_lineWrapLines[i].AsSpan(), fontName, textSize, offset, out _);
            offset += (0, textSize);
        }

        var area = hud.MeasureText(m_saveGameSummary.Date, fontName, textSize);
        //hud.Text(m_saveGameSummary.Date, fontName, textSize, offset, out _);
        m_textAreas.Add(new TextArea(new Box2I(Vec2I.Zero, new Vec2I(0, textSize)), offset, m_saveGameSummary.Date, true));
        offset += (0, area.Height);
        offset += (0, area.Height);

        foreach (string str in m_saveGameSummary.Stats ?? [])
        {
            //area = hud.MeasureText(str, fontName, textSize);
            hud.Text(str, fontName, textSize, offset, out area);
            m_textAreas.Add(new TextArea(new Box2I(Vec2I.Zero, new Vec2I(0, textSize)), offset, str, true));
            offset += (0, area.Height);
        }
        if (m_saveGameSummary.VerificationResult != SaveVerificationResult.Success)
        {
            offset += (0, area.Height);
            var result = GetResultString(m_saveGameSummary.VerificationResult);
            //area = hud.MeasureText(result, fontName, textSize);
            hud.Text(result, fontName, textSize, offset, out _);
            m_textAreas.Add(new TextArea(new Box2I(Vec2I.Zero, new Vec2I(0, textSize)), offset, result, true));
        }
    }

    private static string GetResultString(SaveVerificationResult result)
    {
        return result switch
        {
            SaveVerificationResult.DifferentIwad => "Different IWADs",
            SaveVerificationResult.DifferentFiles => "Different WADs",
            SaveVerificationResult.IncorrectOrder => "Incorrect WAD order",
            _ => "Invalid save",
        };
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Graphics.String;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Commands;
using Helion.Render.Legacy.Texture.Fonts;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;
using Font = Helion.Graphics.Fonts.Font;
using ResolutionScale = Helion.Render.Common.Enums.ResolutionScale;

namespace Helion.Render.Legacy;

public class GLLegacyHudRenderContext : IHudRenderContext
{
    private readonly RenderCommands m_commands;
    private readonly Stack<ResolutionInfo> m_resolutionInfos = new();
    private readonly ArchiveCollection m_archiveCollection;
    public IRendererTextureManager Textures { get; }
    private HudRenderContext? m_context;
    public Dimension WindowDimension { get; } = (800, 600);

    public Dimension Dimension
    {
        get
        {
            if (m_resolutionInfos.TryPeek(out var info))
                return info.VirtualDimensions;
            return m_context?.Dimension ?? (800, 600);
        }
    }

    public GLLegacyHudRenderContext(ArchiveCollection archiveCollection, RenderCommands commands,
        IRendererTextureManager textureManager)
    {
        m_archiveCollection = archiveCollection;
        m_commands = commands;
        Textures = textureManager;
    }

    internal void Begin(HudRenderContext context)
    {
        m_context = context;
    }

    public void Clear(Color color, float alpha)
    {
        if (m_context == null)
            return;

        Dimension dim = m_commands.WindowDimension;
        m_commands.FillRect(new((0, 0), (dim.Vector)), color, alpha);
    }

    public void Point(Vec2I point, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
    {
        // Not implemented in the legacy renderer.
    }

    public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
    {
        // Not implemented in the legacy renderer.
    }

    public void Line(Seg2D seg, Color color, Align window = Align.TopLeft)
    {
        // Not implemented in the legacy renderer.
    }

    public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f)
    {
        if (m_context == null)
            return;

        Vec2I origin = box.Min;
        Dimension dim = box.Dimension;
        Vec2I pos = GetDrawingCoordinateFromAlign(origin.X, origin.Y, dim.Width, dim.Height, window, anchor);

        ImageBox2I imgBox = new(pos, pos + dim.Vector);
        m_commands.FillRect(imgBox, color, alpha);
    }

    public void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1.0f)
    {
        // Not implemented in the legacy renderer.
    }

    public void Image(string texture, HudBox area, out HudBox drawArea, Align window = Align.TopLeft,
        Align anchor = Align.TopLeft, Align? both = null, ResourceNamespace resourceNamespace = ResourceNamespace.Global,
        Color? color = null, float scale = 1.0f, float alpha = 1.0f)
    {
        Image(texture, out drawArea, area, null, window, anchor, both, resourceNamespace, color, scale, alpha);
    }

    public void Image(string texture, Vec2I origin, out HudBox drawArea, Align window = Align.TopLeft,
        Align anchor = Align.TopLeft, Align? both = null, ResourceNamespace resourceNamespace = ResourceNamespace.Global,
        Color? color = null, float scale = 1.0f, float alpha = 1.0f)
    {
        Image(texture, out drawArea, null, origin, window, anchor, both, resourceNamespace, color, scale, alpha);
    }

    private void Image(string texture, out HudBox drawArea, HudBox? area = null, Vec2I? origin = null,
        Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null,
        ResourceNamespace resourceNamespace = ResourceNamespace.Global, Color? color = null,
        float scale = 1.0f, float alpha = 1.0f)
    {
        drawArea = default;

        if (m_context == null)
            return;

        window = both ?? window;
        anchor = both ?? anchor;

        Vec2I location = (origin?.X ?? area?.Left ?? 0, origin?.Y ?? area?.Top ?? 0);
        Dimension drawDim = (0, 0);
        if (area != null)
            drawDim = area.Value.Dimension;
        else if (Textures.TryGet(texture, out var handle))
            drawDim = handle.Dimension;

        drawDim.Scale(scale);

        Vec2I pos = GetDrawingCoordinateFromAlign(location.X, location.Y, drawDim.Width, drawDim.Height,
            window, anchor);

        m_commands.DrawImage(texture, pos.X, pos.Y, drawDim.Width, drawDim.Height,
            color ?? Color.White, alpha, m_context.DrawInvul);

        drawArea = (location, location + drawDim.Vector);
    }

    public void Text(ColoredString text, string font, int fontSize, Vec2I origin, out Dimension drawArea,
        TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft,
        Align? both = null, int maxWidth = Int32.MaxValue, int maxHeight = Int32.MaxValue, float scale = 1.0f,
        float alpha = 1.0f)
    {
        drawArea = default;

        if (m_context == null)
            return;

        window = both ?? window;
        anchor = both ?? anchor;

        Font? fontObject = m_archiveCollection.GetFont(font);
        if (fontObject == null)
            return;

        int scaledFontSize = (int)(fontSize * scale);
        Commands.Alignment.TextAlign legacyAlign = (Commands.Alignment.TextAlign)textAlign;
        RenderableString renderableString = new(text, fontObject, scaledFontSize, legacyAlign, maxWidth);
        drawArea = renderableString.DrawArea;

        Vec2I pos = GetDrawingCoordinateFromAlign(origin.X, origin.Y, drawArea.Width, drawArea.Height,
            window, anchor);

        m_commands.DrawText(renderableString, pos.X, pos.Y, 1.0f);
    }

    public void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea,
        TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft,
        Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, Color? color = null,
        float scale = 1.0f, float alpha = 1.0f)
    {
        drawArea = default;

        if (m_context == null)
            return;

        Font? fontObject = m_archiveCollection.GetFont(font);
        if (fontObject == null)
            return;

        window = both ?? window;
        anchor = both ?? anchor;

        string colorPrefix = "";
        if (color != null)
        {
            Color c = color.Value;
            colorPrefix = @$"\c[{c.R},{c.G},{c.B}]";
        }

        int scaledFontSize = (int)(fontSize * scale);
        Commands.Alignment.TextAlign legacyAlign = (Commands.Alignment.TextAlign)textAlign;
        ColoredString coloredString = RGBColoredStringDecoder.Decode($"{colorPrefix}{text}");
        RenderableString renderableString = new(coloredString, fontObject, scaledFontSize, legacyAlign, maxWidth);
        drawArea = renderableString.DrawArea;

        Vec2I pos = GetDrawingCoordinateFromAlign(origin.X, origin.Y, drawArea.Width, drawArea.Height,
            window, anchor);

        m_commands.DrawText(renderableString, pos.X, pos.Y, 1.0f);
    }

    public Dimension MeasureText(string text, string font, int fontSize, int maxWidth = int.MaxValue,
        int maxHeight = int.MaxValue, float scale = 1.0f)
    {
        const Commands.Alignment.TextAlign align = Commands.Alignment.TextAlign.Left;

        Font? fontObject = m_archiveCollection.GetFont(font);
        if (fontObject == null)
            return default;

        int scaledFontSize = (int)(fontSize * scale);
        ColoredString coloredString = RGBColoredStringDecoder.Decode(text);
        RenderableString renderableString = new(coloredString, fontObject, scaledFontSize, align, maxWidth);
        return renderableString.DrawArea;
    }

    public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null,
        float? aspectRatio = null)
    {
        Commands.ResolutionScale legacyScale = default;

        switch (scale)
        {
        case null:
        case ResolutionScale.None:
            // Already handled.
            break;
        case ResolutionScale.Center:
            legacyScale = Commands.ResolutionScale.Center;
            break;
        case ResolutionScale.Stretch:
            legacyScale = Commands.ResolutionScale.Stretch;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
        }

        float ratio = aspectRatio ?? dimension.AspectRatio;
        ResolutionInfo resolutionInfo = new(dimension, legacyScale, ratio);
        m_commands.SetVirtualResolution(resolutionInfo);
        m_resolutionInfos.Push(resolutionInfo);
    }

    public void PopVirtualDimension()
    {
        if (m_resolutionInfos.TryPop(out _))
        {
            if (!m_resolutionInfos.Empty())
            {
                ResolutionInfo resolutionInfo = m_resolutionInfos.Peek();
                m_commands.SetVirtualResolution(resolutionInfo);
                return;
            }
        }

        ResolutionInfo windowResolutionInfo = new(Dimension, Commands.ResolutionScale.None, Dimension.AspectRatio);
        m_commands.SetVirtualResolution(windowResolutionInfo);
    }

    private Vec2I GetDrawingCoordinateFromAlign(int xOffset, int yOffset, int width, int height,
        Align windowAlign, Align imageAlign)
    {
        Vec2I offset = new Vec2I(xOffset, yOffset);
        Dimension window = Dimension;

        Vec2I windowPos = windowAlign switch
        {
            Align.TopLeft => new Vec2I(0, 0),
            Align.TopMiddle => new Vec2I(window.Width / 2, 0),
            Align.TopRight => new Vec2I(window.Width - 1, 0),
            Align.MiddleLeft => new Vec2I(0, window.Height / 2),
            Align.Center => new Vec2I(window.Width / 2, window.Height / 2),
            Align.MiddleRight => new Vec2I(window.Width - 1, window.Height / 2),
            Align.BottomLeft => new Vec2I(0, window.Height - 1),
            Align.BottomMiddle => new Vec2I(window.Width / 2, window.Height - 1),
            Align.BottomRight => new Vec2I(window.Width - 1, window.Height - 1),
            _ => throw new Exception($"Unsupported window alignment: {windowAlign}")
        };

        // This is relative to the window position.
        Vec2I imageOffset = imageAlign switch
        {
            Align.TopLeft => -new Vec2I(0, 0),
            Align.TopMiddle => -new Vec2I(width / 2, 0),
            Align.TopRight => -new Vec2I(width - 1, 0),
            Align.MiddleLeft => -new Vec2I(0, height / 2),
            Align.Center => -new Vec2I(width / 2, height / 2),
            Align.MiddleRight => -new Vec2I(width - 1, height / 2),
            Align.BottomLeft => -new Vec2I(0, height - 1),
            Align.BottomMiddle => -new Vec2I(width / 2, height - 1),
            Align.BottomRight => -new Vec2I(width - 1, height - 1),
            _ => throw new Exception($"Unsupported image alignment: {imageAlign}")
        };

        return windowPos + imageOffset + offset;
    }

    public void Dispose()
    {
        // Nothing to do
    }
}


﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts.Renderable;
using Helion.Graphics.Geometry;
using Helion.Graphics.String;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Commands;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;
using ResolutionScale = Helion.Render.Common.Enums.ResolutionScale;

namespace Helion.Render.Legacy
{
    public class GLLegacyHudRenderContext : IHudRenderContext
    {
        private readonly RenderCommands m_commands;
        private readonly Stack<ResolutionInfo> m_resolutionInfos = new();
        private readonly ArchiveCollection m_archiveCollection;
        private HudRenderContext? m_context;

        public Dimension Dimension => m_context?.Dimension ?? (800, 600);
        public IRendererTextureManager Textures { get; }

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
            m_commands.DrawImage("NULLWHITE", 0, 0, dim.Width, dim.Height, color, alpha);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            // Not implemented in the legacy renderer.
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            // Not implemented in the legacy renderer.
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            // Not implemented in the legacy renderer.
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
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
            Align anchor = Align.TopLeft, Align? both = null, Color? color = null, float scale = 1.0f, 
            float alpha = 1.0f)
        {
            Image(texture, out drawArea, area, null, window, anchor, both, color, scale, alpha);
        }

        public void Image(string texture, Vec2I origin, out HudBox drawArea, Align window = Align.TopLeft,
            Align anchor = Align.TopLeft, Align? both = null, Color? color = null, float scale = 1.0f,
            float alpha = 1.0f)
        {
            Image(texture, out drawArea, null, origin, window, anchor, both, color, alpha);
        }

        private void Image(string texture, out HudBox drawArea, HudBox? area = null, Vec2I? origin = null, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, Color? color = null, 
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

            Graphics.Fonts.Font? fontObject = m_archiveCollection.GetFontDeprecated(font);
            if (fontObject == null)
                return;
            
            Commands.Alignment.TextAlign legacyAlign = (Commands.Alignment.TextAlign)textAlign;
            RenderableString renderableString = new(text, fontObject, fontSize, legacyAlign, maxWidth);
            m_commands.DrawText(renderableString, origin.X, origin.Y, 1.0f);
            
            drawArea = renderableString.DrawArea;
        }

        public void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea, 
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, Color? color = null, 
            float scale = 1.0f, float alpha = 1.0f)
        {
            drawArea = default;
            
            if (m_context == null)
                return;

            Graphics.Fonts.Font? fontObject = m_archiveCollection.GetFontDeprecated(font);
            if (fontObject == null)
                return;
            
            Commands.Alignment.TextAlign legacyAlign = (Commands.Alignment.TextAlign)textAlign;
            ColoredString coloredString = RGBColoredStringDecoder.Decode(text);
            RenderableString renderableString = new(coloredString, fontObject, fontSize, legacyAlign, maxWidth);
            m_commands.DrawText(renderableString, origin.X, origin.Y, 1.0f);
            
            drawArea = renderableString.DrawArea;
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null)
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
            
            m_commands.SetVirtualResolution(dimension.Width, dimension.Height, legacyScale);

            float aspectRatio = m_context?.Dimension.AspectRatio ?? dimension.AspectRatio;
            ResolutionInfo resolutionInfo = new(dimension, legacyScale, aspectRatio);
            m_resolutionInfos.Push(resolutionInfo);
        }

        public void PopVirtualDimension()
        {
            ResolutionInfo resolutionInfo = new(Dimension, Commands.ResolutionScale.None, Dimension.AspectRatio);

            if (m_resolutionInfos.TryPop(out _))
                if (!m_resolutionInfos.Empty())
                    resolutionInfo = m_resolutionInfos.Peek();

            m_commands.SetVirtualResolution(resolutionInfo);
        }
        
        public void Dispose()
        {
            // Nothing to do
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
    }
}

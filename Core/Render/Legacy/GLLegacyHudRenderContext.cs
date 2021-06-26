using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts.Renderable;
using Helion.Graphics.String;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy.Commands;
using Helion.Resources.Archives.Collection;
using ResolutionScale = Helion.Render.Common.Enums.ResolutionScale;

namespace Helion.Render.Legacy
{
    public class GLLegacyHudRenderContext : IHudRenderContext
    {
        private readonly RenderCommands m_commands;
        private readonly Stack<ResolutionInfo> m_resolutionInfos = new();
        private readonly ArchiveCollection m_archiveCollection;
        private HudRenderContext? m_context;

        public GLLegacyHudRenderContext(ArchiveCollection archiveCollection, RenderCommands commands)
        {
            m_archiveCollection = archiveCollection;
            m_commands = commands;
        }
        
        internal void Begin(HudRenderContext context)
        {
            m_context = context;
        }

        public void Clear(Color color)
        {
            if (m_context == null)
                return;

            Box2I screen = ((0, 0), m_commands.WindowDimension.Vector);
            FillBox(screen, color);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft)
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

        public void DrawBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void FillBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            if (m_context == null)
                return;
            
            Vec2I pos = box.Min;
            Dimension dim = box.Dimension;
            m_commands.DrawImage("NULL", pos.X, pos.Y, dim.Width, dim.Height, color);
        }

        public void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void Image(string texture, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1)
        {
            if (m_context == null)
                return;
            
            Dimension dim = m_commands.ImageDrawInfoProvider.GetImageDimension(texture);
            m_commands.DrawImage(texture, origin.X, origin.Y, dim.Width, dim.Height, 
                color ?? Color.White, alpha);
        }

        public void Text(string text, string fontName, int fontSize, Vec2I origin, TextAlign textAlign = TextAlign.Left, 
            Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, 
            int maxHeight = int.MaxValue, Color? color = null, float alpha = 1.0f)
        {
            if (m_context == null)
                return;

            Graphics.Fonts.Font? font = m_archiveCollection.GetFontDeprecated(fontName);
            if (font == null)
                return;
            
            Commands.Alignment.TextAlign legacyAlign = (Commands.Alignment.TextAlign)textAlign;
            ColoredString coloredString = RGBColoredStringDecoder.Decode(text);
            RenderableString renderableString = new(coloredString, font, fontSize, legacyAlign, maxWidth);
            m_commands.DrawText(renderableString, origin.X, origin.Y, 1.0f);
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
        }

        public void PopVirtualDimension()
        {
            m_resolutionInfos.TryPop(out _);
        }
        
        public void Dispose()
        {
            // Nothing to do
        }
    }
}

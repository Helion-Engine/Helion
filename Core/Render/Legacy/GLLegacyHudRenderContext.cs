using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts.Renderable;
using Helion.Graphics.String;
using Helion.Render.Common;
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

        public Dimension Dimension => m_context?.Dimension ?? (800, 600);

        public GLLegacyHudRenderContext(ArchiveCollection archiveCollection, RenderCommands commands)
        {
            m_archiveCollection = archiveCollection;
            m_commands = commands;
        }
        
        internal void Begin(HudRenderContext context)
        {
            m_context = context;
        }

        public bool ImageExists(string name) => m_commands.ImageDrawInfoProvider.ImageExists(name);

        public void Clear(Color color)
        {
            if (m_context == null)
                return;

            HudBox screen = ((0, 0), m_commands.WindowDimension.Vector);
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

        public void DrawBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void DrawBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft)
        {
            if (m_context == null)
                return;
            
            Vec2I pos = box.Min;
            Dimension dim = box.Dimension;
            m_commands.DrawImage("NULL", pos.X, pos.Y, dim.Width, dim.Height, color);
        }

        public void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft)
        {
            // Not implemented in the legacy renderer.
        }

        public void Image(string texture, out HudBox drawArea, HudBox? area = null, Vec2I? origin = null, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, Color? color = null, 
            float alpha = 1.0f)
        {
            drawArea = default;
            
            if (m_context == null)
                return;

            int x = origin?.X ?? area?.Left ?? 0;
            int y = origin?.Y ?? area?.Bottom ?? 0;
            Dimension dim = m_commands.ImageDrawInfoProvider.GetImageDimension(texture);
            
            m_commands.DrawImage(texture, x, y, dim.Width, dim.Height, color ?? Color.White, alpha);
        }

        public void Text(ColoredString text, string font, int fontSize, Vec2I origin, out Dimension drawArea,
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft,
            Align? both = null, int maxWidth = Int32.MaxValue, int maxHeight = Int32.MaxValue, float alpha = 1)
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
            float alpha = 1.0f)
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

using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Resources;
using Helion.Util.Extensions;
using Helion.Util.Timing;

namespace Helion.Layer.EndGame
{
    public partial class EndGameLayer
    {
        private const string FontName = "SMALLFONT";
        private static readonly Vec2I TextStartCorner = new(24, 4);
        
        private IList<string> m_images = Array.Empty<string>();
        private Vec2I m_theEndOffset = Vec2I.Zero;
        private bool m_initRenderPages;
        private uint m_charsToDraw;

        public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
        {
            hud.Clear(Color.Black);

            if (!m_initRenderPages)
            {
                SetPage(hud);
                
                if (TheEndImages.Count > 0)
                {
                    if (hud.Textures.TryGet(TheEndImages[0], out var handle))
                        m_theEndOffset.Y = -handle.Dimension.Height;
                }
            }
            
            if (m_drawState <= EndGameDrawState.TextComplete)
            {
                bool showAllText = m_drawState > EndGameDrawState.Text;
                Draw(m_flatImage, m_displayText, m_ticker, showAllText, hud, ctx);
            }
            else
            {
                hud.DoomVirtualResolution(() =>
                {
                    DrawBackgroundImages(m_images, m_xOffset, hud, ctx);

                    if (m_drawState == EndGameDrawState.TheEnd)
                        hud.Image(TheEndImages[m_theEndImageIndex], m_theEndOffset, Align.Center);
                });
            }
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
            else
            {
                var pages = LayerUtil.GetRenderPages(hud, m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.CreditPages, false);
                if (pages.Count > 0)
                    m_images = new[] { pages[^1] };
            }
        }

        private void Draw(string flat, IList<string> displayText, Ticker ticker, bool showAllText, IHudRenderContext hud,
            IRenderableSurfaceContext ctx)
        {
            ctx.ClearDepth();
            hud.Clear(Color.Black);
            
            hud.DoomVirtualResolution(() =>
            {
                DrawBackground(flat, hud);
                DrawText(displayText, ticker, showAllText, hud);
            });
        }
        
        private void DrawBackgroundImages(IList<string> images, int xOffset, IHudRenderContext hud,
            IRenderableSurfaceContext ctx)
        {
            ctx.ClearDepth();
            hud.Clear(Color.Black);

            for (int i = 0; i < images.Count; i++)
            {
                string image = images[i];
                if (!hud.Textures.TryGet(image, out var handle))
                    continue;
                
                hud.Image(image, (xOffset, 0));
                xOffset -= handle.Dimension.Width;
            }
        }
        
        private static void DrawBackground(string flat, IHudRenderContext hud)
        {
            if (!hud.Textures.TryGet(flat, out var flatHandle, ResourceNamespace.Flats))
                return;

            var (width, height) = flatHandle.Dimension;
            int repeatX = hud.Dimension.Width / width;
            int repeatY = hud.Dimension.Height / height;
        
            if (hud.Dimension.Width % width != 0)
                repeatX++;
            if (hud.Dimension.Height % height != 0)
                repeatY++;
        
            Vec2I drawCoordinate = (0, 0);
            for (int y = 0; y < repeatY; y++)
            {
                for (int x = 0; x < repeatX; x++)
                {
                    hud.Image(flat, drawCoordinate);
                    drawCoordinate.X += width;
                }
                
                drawCoordinate.X = 0;
                drawCoordinate.Y += height;
            }
        }
        
        private void DrawText(IEnumerable<string> lines, Ticker ticker, bool showAllText, IHudRenderContext hud)
        {
            const int LineSpacing = 4;

            Graphics.Fonts.Font? font = m_archiveCollection.GetFontDeprecated(FontName);
            if (font == null)
                return;

            // The ticker goes slower than normal, so as long as we see one
            // or more ticks happening then advance the number of characters
            // to draw.
            m_charsToDraw += (uint)ticker.GetTickerInfo().Ticks;
            
            int charsDrawn = 0;
            int x = TextStartCorner.X;
            int y = TextStartCorner.Y;
            int fontSize = font.MaxHeight - 1;
        
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
                y += fontSize + LineSpacing;
            }
        }
    }
}

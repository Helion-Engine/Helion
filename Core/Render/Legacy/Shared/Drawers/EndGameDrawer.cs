using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Legacy.Commands;
using Helion.Render.Legacy.Shared.Drawers.Helper;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Timing;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Legacy.Shared.Drawers
{
    public class EndGameDrawer
    {
        private const string Font = "SMALLFONT";
        private static readonly Vec2I TextStartCorner = new(24, 4);
        private static readonly ResolutionInfo Resolution = DoomHudHelper.DoomResolutionInfoCenter;
        
        private readonly ArchiveCollection m_archiveCollection;
        private uint m_charsToDraw;

        public EndGameDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;          
        }

        public void Draw(string flat, IList<string> displayText, Ticker ticker, bool showAllText, RenderCommands renderCommands, DrawHelper draw)
        {
            renderCommands.ClearDepth();

            draw.FillWindow(Color.Black);
            draw.AtResolution(Resolution, () =>
            {
                DrawBackground(flat, draw);
                DrawText(displayText, ticker, showAllText, draw);
            });
        }

        public void DrawBackgroundImages(IList<string> images, int xOffset, RenderCommands renderCommands, DrawHelper draw)
        {
            renderCommands.ClearDepth();

            draw.FillWindow(Color.Black);

            int widthDrawn = 0;

            foreach (string image in images)
            {
                var area = draw.DrawInfoProvider.GetImageDimension(image);
                draw.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
                {
                    renderCommands.DrawImage(image, xOffset, 0, area.Width, area.Height, Color.White);
                });

                xOffset -= area.Width;
                widthDrawn += area.Width + xOffset;
            }
        }

        private static void DrawBackground(string flat, DrawHelper helper)
        {
            // TODO: This assumes 64 x 64 textures. It is not robust at all.
            var dimension = helper.DrawInfoProvider.GetImageDimension(flat, ResourceNamespace.Flats);
            int repeatX = Resolution.VirtualDimensions.Width / dimension.Width;
            int repeatY = Resolution.VirtualDimensions.Height / dimension.Height;

            if (Resolution.VirtualDimensions.Width % dimension.Width != 0)
                repeatX++;
            if (Resolution.VirtualDimensions.Height % dimension.Height != 0)
                repeatY++;

            Vec2I drawCoordinate = Vec2I.Zero;
            for (int y = 0; y < repeatY; y++)
            {
                for (int x = 0; x < repeatX; x++)
                {
                    helper.Image(flat, drawCoordinate);
                    drawCoordinate.X += dimension.Width;
                }
                
                drawCoordinate.X = 0;
                drawCoordinate.Y += dimension.Height;
            }
        }

        private void DrawText(IEnumerable<string> lines, Ticker ticker, bool showAllText, DrawHelper helper)
        {
            const int LineSpacing = 4;
            
            Font? font = m_archiveCollection.GetFontDeprecated(Font);
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

            foreach (string line in lines)
            {
                foreach (char c in line)
                {
                    if (!showAllText && charsDrawn >= m_charsToDraw)
                        return;
                    
                    helper.Text(Color.Red, c.ToString(), font, fontSize, out Dimension drawArea, x, y);
                    x += drawArea.Width;
                    
                    charsDrawn++;
                }

                x = TextStartCorner.X;
                y += fontSize + LineSpacing;
            }
        }
    }
}

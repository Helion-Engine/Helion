using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Timing;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Shared.Drawers
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

        public void Draw(string flat, IList<string> displayText, Ticker ticker, bool showAllText, RenderCommands renderCommands)
        {
            DrawHelper helper = new(renderCommands);

            renderCommands.ClearDepth();
            
            helper.FillWindow(Color.Black);

            helper.AtResolution(Resolution, () =>
            {
                DrawBackground(flat, helper);
                DrawText(displayText, ticker, showAllText, helper);
            });
        }

        private static void DrawBackground(string flat, DrawHelper helper)
        {
            // TODO: This assumes 64 x 64 textures. It is not robust at all.
            var dimension = helper.DrawInfoProvider.GetImageDimension(flat, ResourceNamespace.Flats);
            int repeatX = Resolution.VirtualDimensions.Width / dimension.Width;
            int repeatY = Resolution.VirtualDimensions.Height / dimension.Height;

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
            
            Font? font = m_archiveCollection.GetFont(Font);
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

using System.Collections.Generic;
using System.Drawing;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Timing;
using static Helion.Util.Assertion.Assert;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Shared.Drawers
{
    public class EndGameDrawer
    {
        private const string Font = "SMALLFONT";
        private static readonly Vec2I TextStartCorner = new(8, 8);
        private static readonly ResolutionInfo Resolution = DoomHudHelper.DoomResolutionInfo;
        
        private readonly ArchiveCollection m_archiveCollection;

        public EndGameDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public void Draw(ClusterDef cluster, bool drawPic, Ticker ticker, RenderCommands renderCommands)
        {
            DrawHelper helper = new(renderCommands);

            renderCommands.ClearDepth();

            helper.AtResolution(Resolution, () =>
            {
                if (drawPic)
                    DrawPic(cluster.Pic, helper);
                else
                {
                    DrawBackground(cluster.Flat, helper);
                    DrawText(cluster.EnterText, ticker, helper);    
                }
            });
        }

        private static void DrawPic(string image, DrawHelper helper)
        {
            Precondition(!image.Empty(), "Should not be drawing an empty image");
            
            (int w, int h) = Resolution.VirtualDimensions;
            helper.Image(image, 0, 0, w, h);
        }

        private static void DrawBackground(string flat, DrawHelper helper)
        {
            var dimension = helper.DrawInfoProvider.GetImageDimension(flat, ResourceNamespace.Flats);

            int repeatX = (Resolution.VirtualDimensions.Width / dimension.Width) + 1;
            int repeatY = (Resolution.VirtualDimensions.Height / dimension.Height) + 1;

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

        private static int CalculateCharactersToDraw(Ticker ticker)
        {
            const int ticksPerLetter = 5;
            return ticker.GetTickerInfo().Ticks / ticksPerLetter;
        }
        
        private void DrawText(IEnumerable<string> lines, Ticker ticker, DrawHelper helper)
        {
            const int LineSpacing = 4;
            
            Font? font = m_archiveCollection.GetFont(Font);
            if (font == null)
                return;
            
            int charsDrawn = 0;
            int charsToDraw = CalculateCharactersToDraw(ticker);
            int x = TextStartCorner.X;
            int y = TextStartCorner.Y;
            int fontSize = font.MaxHeight;

            foreach (string line in lines)
            {
                foreach (char c in line)
                {
                    if (charsDrawn > charsToDraw)
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

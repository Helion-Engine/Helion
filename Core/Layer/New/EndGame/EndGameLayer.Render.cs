using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Renderers;
using Helion.Util.Extensions;

namespace Helion.Layer.New.EndGame
{
    public partial class EndGameLayer
    {
        private const string Font = "SMALLFONT";
        private static readonly Vec2I TextStartCorner = new(24, 4);
        
        private IList<string> m_images = Array.Empty<string>();
        private Vec2I m_theEndOffset = Vec2I.Zero;
        private bool m_initRenderPages;

        public void Render(IHudRenderContext hud)
        {
            // if (!m_initRenderPages)
            // {
            //     SetPage(hud);
            //     
            //     // TODO:
            //     if (TheEndImages.Count > 0)
            //         m_theEndOffset.Y = -draw.DrawInfoProvider.GetImageDimension(TheEndImages[0]).Height;
            // }
            //
            // if (m_drawState <= EndGameDrawState.TextComplete)
            // {
            //     bool showAllText = m_drawState > EndGameDrawState.Text;
            //     
            //     // TODO:
            //     m_drawer.Draw(m_flatImage, m_displayText, m_ticker, showAllText, renderCommands, draw);
            // }
            // else
            // {
            //     // TODO:
            //     m_drawer.DrawBackgroundImages(m_images, m_xOffset, renderCommands, draw);
            //     if (m_drawState == EndGameDrawState.TheEnd)
            //     {
            //         hud.DoomVirtualResolution(() =>
            //         {
            //             // TODO:
            //             draw.Image(TheEndImages[m_theEndImageIndex], m_theEndOffset.X, m_theEndOffset.Y, window: Align.Center);
            //         });
            //     }
            // }
        }
        
        private void SetPage(IHudRenderContext hud)
        {
            // m_initRenderPages = true;
            //
            // string next = World.MapInfo.Next;
            // if (next.EqualsIgnoreCase("EndPic"))
            // {
            //     m_images = new[] { World.MapInfo.EndPic };
            // }
            // else if (next.EqualsIgnoreCase("EndGame2"))
            // {
            //     m_images = new[] { "VICTORY2" };
            // }
            // else if (next.EqualsIgnoreCase("EndGame3") || next.EqualsIgnoreCase("EndBunny"))
            // {
            //     m_images = new[] { "PFUB1", "PFUB2" };
            //     m_xOffsetStop = draw.DrawInfoProvider.GetImageDimension(m_images[0]).Width;
            //     m_shouldScroll = true;
            // }
            // else if (next.EqualsIgnoreCase("EndGame4"))
            // {
            //     m_images = new[] { "ENDPIC" };
            // }
            // else
            // {
            //     var pages = LayerUtil.GetRenderPages(hud, m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.CreditPages, false);
            //     if (pages.Count > 0)
            //         m_images = new[] { pages[^1] };
            // }
        }

        // public void Draw(string flat, IList<string> displayText, Ticker ticker, bool showAllText, RenderCommands renderCommands, DrawHelper draw)
        // {
        //     renderCommands.ClearDepth();
        //
        //     draw.FillWindow(Color.Black);
        //     draw.AtResolution(Resolution, () =>
        //     {
        //         DrawBackground(flat, draw);
        //         DrawText(displayText, ticker, showAllText, draw);
        //     });
        // }
        //
        // public void DrawBackgroundImages(IList<string> images, int xOffset, RenderCommands renderCommands, DrawHelper draw)
        // {
        //     renderCommands.ClearDepth();
        //
        //     draw.FillWindow(Color.Black);
        //
        //     int widthDrawn = 0;
        //
        //     foreach (string image in images)
        //     {
        //         var area = draw.DrawInfoProvider.GetImageDimension(image);
        //         draw.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
        //         {
        //             renderCommands.DrawImage(image, xOffset, 0, area.Width, area.Height, Color.White);
        //         });
        //
        //         xOffset -= area.Width;
        //         widthDrawn += area.Width + xOffset;
        //     }
        // }
        //
        // private static void DrawBackground(string flat, DrawHelper helper)
        // {
        //     // TODO: This assumes 64 x 64 textures. It is not robust at all.
        //     var dimension = helper.DrawInfoProvider.GetImageDimension(flat, ResourceNamespace.Flats);
        //     int repeatX = Resolution.VirtualDimensions.Width / dimension.Width;
        //     int repeatY = Resolution.VirtualDimensions.Height / dimension.Height;
        //
        //     if (Resolution.VirtualDimensions.Width % dimension.Width != 0)
        //         repeatX++;
        //     if (Resolution.VirtualDimensions.Height % dimension.Height != 0)
        //         repeatY++;
        //
        //     Vec2I drawCoordinate = Vec2I.Zero;
        //     for (int y = 0; y < repeatY; y++)
        //     {
        //         for (int x = 0; x < repeatX; x++)
        //         {
        //             helper.Image(flat, drawCoordinate);
        //             drawCoordinate.X += dimension.Width;
        //         }
        //         
        //         drawCoordinate.X = 0;
        //         drawCoordinate.Y += dimension.Height;
        //     }
        // }
        //
        // private void DrawText(IEnumerable<string> lines, Ticker ticker, bool showAllText, DrawHelper helper)
        // {
        //     const int LineSpacing = 4;
        //     
        //     Font? font = m_archiveCollection.GetFontDeprecated(Font);
        //     if (font == null)
        //         return;
        //
        //     // The ticker goes slower than normal, so as long as we see one
        //     // or more ticks happening then advance the number of characters
        //     // to draw.
        //     m_charsToDraw += (uint)ticker.GetTickerInfo().Ticks;
        //     
        //     int charsDrawn = 0;
        //     int x = TextStartCorner.X;
        //     int y = TextStartCorner.Y;
        //     int fontSize = font.MaxHeight - 1;
        //
        //     foreach (string line in lines)
        //     {
        //         foreach (char c in line)
        //         {
        //             if (!showAllText && charsDrawn >= m_charsToDraw)
        //                 return;
        //             
        //             helper.Text(Color.Red, c.ToString(), font, fontSize, out Dimension drawArea, x, y);
        //             x += drawArea.Width;
        //             
        //             charsDrawn++;
        //         }
        //
        //         x = TextStartCorner.X;
        //         y += fontSize + LineSpacing;
        //     }
        // }
    }
}

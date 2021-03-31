using System.Drawing;
using Helion.Layer;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Geometry;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Shared.Drawers
{
    public class IntermissionDrawer
    {
        private const string IntermissionPic = "INTERPIC";
        private const string Font = "IntermissionFont";
        private const int FontSize = 12;
        private static readonly ResolutionInfo Resolution = DoomHudHelper.DoomResolutionInfoCenter;
        
        private readonly ArchiveCollection m_archiveCollection;
        private readonly MapInfoDef m_mapInfo;

        public IntermissionDrawer(ArchiveCollection archiveCollection, MapInfoDef mapInfo)
        {
            m_archiveCollection = archiveCollection;
            m_mapInfo = mapInfo;
        }

        public void Draw(IntermissionLayer layer, RenderCommands commands)
        {
            DrawHelper draw = new(commands);
            Font? intermissionFont = m_archiveCollection.GetFont(Font);

            commands.ClearDepth();

            draw.FillWindow(Color.Black);
            
            draw.AtResolution(Resolution, () =>
            {
                draw.Image(IntermissionPic, 0, 0);

                DrawTitle(draw);
                DrawStatistics(draw, layer, intermissionFont);
                DrawTime(draw, layer, intermissionFont);
            });
        }

        private void DrawTitle(DrawHelper draw)
        {
            const string FinishedImage = "WIF";
            const int topPaddingY = 4;
            
            draw.Image(m_mapInfo.TitlePatch, 0, topPaddingY, out Dimension drawArea, both: Align.TopMiddle);
            draw.Image(FinishedImage, 0, drawArea.Height + topPaddingY + 1, both: Align.TopMiddle);
        }

        private void DrawStatistics(DrawHelper draw, IntermissionLayer layer, Font? intermissionFont)
        {
            const int LeftOffsetX = 40;
            const int RightOffsetX = 280;
            const int OffsetY = 50;
            const int RowOffsetY = 18;

            draw.Image("WIOSTK", LeftOffsetX, OffsetY);
            draw.Image("WIOSTI", LeftOffsetX, OffsetY + RowOffsetY);
            draw.Image("WISCRT2", LeftOffsetX, OffsetY + (2 * RowOffsetY));

            if (intermissionFont != null)
            {
                DrawNumber(layer.KillPercent, OffsetY);
                DrawNumber(layer.ItemPercent, OffsetY + RowOffsetY);
                DrawNumber(layer.SecretPercent, OffsetY + (2 * RowOffsetY));
            }

            void DrawNumber(double percent, int offsetY)
            {
                string text = $"{(int)(percent * 100)}%";
                (int w, int _) = draw.TextDrawArea(text, intermissionFont, FontSize);
                
                // TODO: Use TextAlign.Right
                draw.Text(Color.White, text, intermissionFont, FontSize, RightOffsetX - w, offsetY);
            }
        }

        private void DrawTime(DrawHelper draw, IntermissionLayer intermissionLayer, Font? intermissionFont)
        {
        }
    }
}

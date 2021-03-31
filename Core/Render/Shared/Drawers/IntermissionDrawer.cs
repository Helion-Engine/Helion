using System.Drawing;
using Helion.Layer;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
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
        private readonly MapInfoDef m_currentMapInfo;
        private readonly MapInfoDef m_nextMapInfo;

        public IntermissionDrawer(ArchiveCollection archiveCollection, MapInfoDef currentMapInfo,
            MapInfoDef nextMapInfo)
        {
            m_archiveCollection = archiveCollection;
            m_currentMapInfo = currentMapInfo;
            m_nextMapInfo = nextMapInfo;
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

                DrawTitle(draw, layer);
                DrawStatistics(draw, layer, intermissionFont);
                DrawTime(draw, layer, intermissionFont);
            });
        }

        private void DrawTitle(DrawHelper draw, IntermissionLayer layer)
        {
            const string FinishedImage = "WIF";
            const string NowEnteringImage = "WIENTER";
            const int topPaddingY = 4;

            if (layer.IntermissionState == IntermissionState.NextMap)
            {
                draw.Image(NowEnteringImage, 0, topPaddingY, out Dimension drawArea, both: Align.TopMiddle);
                draw.Image(m_nextMapInfo.TitlePatch, 0, drawArea.Height + topPaddingY + 1, both: Align.TopMiddle);
            }
            else
            {
                draw.Image(m_currentMapInfo.TitlePatch, 0, topPaddingY, out Dimension drawArea, both: Align.TopMiddle);
                draw.Image(FinishedImage, 0, drawArea.Height + topPaddingY + 1, both: Align.TopMiddle);   
            }
        }

        private void DrawStatistics(DrawHelper draw, IntermissionLayer layer, Font? intermissionFont)
        {
            const int LeftOffsetX = 40;
            const int RightOffsetX = 280;
            const int OffsetY = 50;
            const int RowOffsetY = 18;

            if (layer.IntermissionState == IntermissionState.NextMap)
                return;

            if (layer.IntermissionState >= IntermissionState.TallyingKills)
            {
                draw.Image("WIOSTK", LeftOffsetX, OffsetY);
                DrawNumber(layer.KillPercent, OffsetY, intermissionFont);
            }
            
            if (layer.IntermissionState >= IntermissionState.TallyingItems)
            {
                draw.Image("WIOSTI", LeftOffsetX, OffsetY + RowOffsetY);
                DrawNumber(layer.ItemPercent, OffsetY + RowOffsetY, intermissionFont);
            }
            
            if (layer.IntermissionState >= IntermissionState.TallyingSecrets)
            {
                draw.Image("WISCRT2", LeftOffsetX, OffsetY + (2 * RowOffsetY));
                DrawNumber(layer.SecretPercent, OffsetY + (2 * RowOffsetY), intermissionFont);
            }

            void DrawNumber(double percent, int offsetY, Font? font)
            {
                if (font == null)
                    return;
                
                string text = $"{(int)(percent * 100)}%";
                (int w, int _) = draw.TextDrawArea(text, font, FontSize);
                
                // TODO: Use TextAlign.Right
                draw.Text(Color.White, text, font, FontSize, RightOffsetX - w, offsetY);
            }
        }

        private void DrawTime(DrawHelper draw, IntermissionLayer layer, Font? renderFont)
        {
            const int LeftOffsetTimeX = 40;
            const int RightOffsetLevelTimeX = 150;
            const int LeftOffsetParX = 180;
            const int RightOffsetParTimeX = 280;
            const int OffsetY = 40;
            
            if (layer.IntermissionState == IntermissionState.NextMap || layer.IntermissionState < IntermissionState.ShowingPar)
                return;
            
            draw.Image("WITIME", LeftOffsetTimeX, -OffsetY, window: Align.BottomLeft);
            draw.Image("WIPAR", LeftOffsetParX, -OffsetY, window: Align.BottomLeft);

            int levelTimeSeconds = (int)(layer.World.LevelTime / Constants.TicksPerSecond);
            RenderTime(levelTimeSeconds, RightOffsetLevelTimeX, renderFont);
            RenderTime(m_currentMapInfo.ParTime, RightOffsetParTimeX, renderFont);

            string GetTimeString(int seconds)
            {
                int minutes = seconds / 60;
                string secondsStr = (seconds % 60).ToString().PadLeft(2, '0');
                return $"{minutes}:{secondsStr}";
            }

            void RenderTime(int seconds, int rightOffsetX, Font? font)
            {
                if (font == null)
                    return;
                
                // TODO: Use TextAlign.Right for both below.
                string levelTime = GetTimeString(seconds);
                (int w, int _) = draw.TextDrawArea(levelTime, font, FontSize);
                draw.Text(Color.White, levelTime, font, FontSize, rightOffsetX - w, -OffsetY, window: Align.BottomLeft);
            }
        }
    }
}

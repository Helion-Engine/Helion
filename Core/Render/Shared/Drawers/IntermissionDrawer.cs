using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Layer;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Definitions.Intermission;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.World;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Shared.Drawers
{
    public class IntermissionDrawer
    {
        private const string Font = "IntermissionFont";
        private const int FontSize = 12;
        private static readonly ResolutionInfo Resolution = DoomHudHelper.DoomResolutionInfoCenter;

        private readonly IWorld m_world;
        private readonly MapInfoDef m_currentMapInfo;
        private readonly MapInfoDef? m_nextMapInfo;
        private readonly List<IntermissionSpot> m_visitedSpots = new();
        private IntermissionSpot? m_nextSpot;
        private string? m_pointerImage;

        private int m_lastPointerTic;
        private bool m_drawPointer;
        private bool m_spotsInit;

        public IntermissionDrawer(IWorld world, MapInfoDef currentMapInfo,
            MapInfoDef? nextMapInfo, IntermissionLayer layer)
        {
            m_world = world;
            m_currentMapInfo = currentMapInfo;
            m_nextMapInfo = nextMapInfo;
        }    

        public void Draw(IntermissionLayer layer, RenderCommands commands, int tics)
        {
            DrawHelper draw = new(commands);
            Font? intermissionFont = m_world.ArchiveCollection.GetFont(Font);

            commands.ClearDepth();

            draw.FillWindow(Color.Black);
            
            draw.AtResolution(Resolution, () =>
            {
                draw.Image(layer.IntermissionPic, 0, 0);
                DrawAnimations(draw, layer);
                DrawPointer(draw, layer, tics);

                DrawTitle(draw, layer);
                DrawStatistics(draw, layer, intermissionFont);
                DrawTime(draw, layer, intermissionFont);
            });
        }

        private void DrawPointer(DrawHelper draw, IntermissionLayer layer, int tics)
        {
            if (layer.IntermissionState < IntermissionState.NextMap || layer.IntermissionDef == null)
                return;

            if (!m_spotsInit)
            {
                InitSpots(draw, layer);
                m_spotsInit = true;
            }

            if (tics - m_lastPointerTic >= (m_drawPointer ? 20 : 11))
            {
                m_drawPointer = !m_drawPointer;
                m_lastPointerTic = tics;
            }

            foreach (var visitedSpot in m_visitedSpots)
                draw.Image(layer.IntermissionDef.Splat, visitedSpot.Box.BottomLeft.X, visitedSpot.Box.BottomLeft.Y);

            if (m_drawPointer && m_nextSpot != null && m_pointerImage != null)
                draw.Image(m_pointerImage, m_nextSpot.Box.BottomLeft.X, m_nextSpot.Box.BottomRight.Y);
        }

        private void InitSpots(DrawHelper draw, IntermissionLayer layer)
        {
            if (layer.IntermissionDef != null)
            {
                var dimension = draw.DrawInfoProvider.GetImageDimension(layer.IntermissionDef.Splat);
                var offset = draw.DrawInfoProvider.GetImageOffset(layer.IntermissionDef.Splat);
                draw.TranslateDoomOffset(ref offset, dimension);

                foreach (var visitedMap in m_world.GlobalData.VisitedMaps)
                {
                    var spot = layer.IntermissionDef.Spots.FirstOrDefault(x => x.MapName.Equals(visitedMap.MapName, StringComparison.OrdinalIgnoreCase));
                    if (spot != null)
                    {
                        m_visitedSpots.Add(spot);
                        var spotOffset = offset;
                        spotOffset.X += spot.X;
                        spotOffset.Y += spot.Y;
                        spot.Box = new Box2I(spotOffset, new Vec2I(spotOffset.X + dimension.Width, spotOffset.Y + dimension.Height));
                    }
                }

                m_nextSpot = layer.NextMapInfo == null ? null : layer.IntermissionDef.Spots.FirstOrDefault(x => x.MapName == layer.NextMapInfo.MapName);
                if (m_nextSpot != null && layer.IntermissionDef.Pointer.Count > 1)
                {
                    m_pointerImage = layer.IntermissionDef.Pointer[0];
                    var nextSpotDimension = draw.DrawInfoProvider.GetImageDimension(layer.IntermissionDef.Pointer[0]);
                    var nextSpotOffset = draw.DrawInfoProvider.GetImageOffset(layer.IntermissionDef.Pointer[0]);
                    draw.TranslateDoomOffset(ref nextSpotOffset, nextSpotDimension);
                    nextSpotOffset.X += m_nextSpot.X;
                    nextSpotOffset.Y += m_nextSpot.Y;
                    m_nextSpot.Box = new Box2I(nextSpotOffset, new Vec2I(nextSpotOffset.X + nextSpotDimension.Width,
                        nextSpotOffset.Y + nextSpotDimension.Height));
                }
            }
        }

        private static void DrawAnimations(DrawHelper draw, IntermissionLayer layer)
        {
            if (layer.IntermissionDef == null)
                return;

            foreach (var animation in layer.IntermissionDef.Animations)
            {
                if (animation.ShouldDraw)
                {
                    string image = animation.Items[animation.ItemIndex];
                    Vec2I offset = draw.DrawInfoProvider.GetImageOffset(image);
                    draw.TranslateDoomOffset(ref offset, draw.DrawInfoProvider.GetImageDimension(image));
                    draw.Image(image, animation.X + offset.X, animation.Y + offset.Y);
                }
            }
        }

        private void DrawTitle(DrawHelper draw, IntermissionLayer layer)
        {
            const string FinishedImage = "WIF";
            const string NowEnteringImage = "WIENTER";
            const int topPaddingY = 4;

            if (layer.IntermissionState >= IntermissionState.NextMap && m_nextMapInfo != null)
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

        private static void DrawStatistics(DrawHelper draw, IntermissionLayer layer, Font? intermissionFont)
        {
            const int LeftOffsetX = 40;
            const int RightOffsetX = 280;
            const int OffsetY = 50;
            const int RowOffsetY = 18;

            if (layer.IntermissionState >= IntermissionState.NextMap)
                return;

            draw.Image("WIOSTK", LeftOffsetX, OffsetY);
            draw.Image("WIOSTI", LeftOffsetX, OffsetY + RowOffsetY);
            draw.Image("WISCRT2", LeftOffsetX, OffsetY + (2 * RowOffsetY));

            if (layer.IntermissionState >= IntermissionState.TallyingKills)
                DrawNumber(layer.KillPercent, OffsetY, intermissionFont);
            
            if (layer.IntermissionState >= IntermissionState.TallyingItems)
                DrawNumber(layer.ItemPercent, OffsetY + RowOffsetY, intermissionFont);
            
            if (layer.IntermissionState >= IntermissionState.TallyingSecrets)
                DrawNumber(layer.SecretPercent, OffsetY + (2 * RowOffsetY), intermissionFont);

            void DrawNumber(double percent, int offsetY, Font? font)
            {
                if (font == null)
                    return;
                
                string text = $"{percent}%";
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
            const int TotalOffsetY = 20;
            
            if (layer.IntermissionState >= IntermissionState.NextMap || layer.IntermissionState < IntermissionState.TallyingTime)
                return;
            
            draw.Image("WITIME", LeftOffsetTimeX, -OffsetY, window: Align.BottomLeft);
            draw.Image("WIPAR", LeftOffsetParX, -OffsetY, window: Align.BottomLeft);

            RenderTime(layer.LevelTimeSeconds, RightOffsetLevelTimeX, -OffsetY, renderFont);
            RenderTime(layer.ParTimeSeconds, RightOffsetParTimeX, -OffsetY, renderFont);

            if (layer.IntermissionState >= IntermissionState.ShowAllStats)
            {
                draw.Image("WIMSTT", LeftOffsetTimeX, -TotalOffsetY, window: Align.BottomLeft);
                RenderTime(layer.World.GlobalData.TotalTime / (int)Constants.TicksPerSecond, RightOffsetLevelTimeX, -TotalOffsetY, renderFont);
            }

            string GetTimeString(int seconds)
            {
                int minutes = seconds / 60;
                string secondsStr = (seconds % 60).ToString().PadLeft(2, '0');
                return $"{minutes}:{secondsStr}";
            }

            void RenderTime(int seconds, int rightOffsetX, int y, Font? font)
            {
                if (font == null)
                    return;
                
                // TODO: Use TextAlign.Right for both below.
                string levelTime = GetTimeString(seconds);
                (int w, int _) = draw.TextDrawArea(levelTime, font, FontSize);
                draw.Text(Color.White, levelTime, font, FontSize, rightOffsetX - w, y, window: Align.BottomLeft);
            }
        }
    }
}

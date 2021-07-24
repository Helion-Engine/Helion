using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Resources.Definitions.Intermission;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds
{
    public partial class IntermissionLayer
    {
        private const int FontSize = 12;
        private const string Font = "IntermissionFont";

        private readonly List<IntermissionSpot> m_visitedSpots = new();
        private IntermissionSpot? m_nextSpot;
        private string? m_pointerImage;
        private int m_lastPointerTic;
        private bool m_drawPointer;
        private bool m_spotsInit;
        
        public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
        {
            ctx.ClearDepth();
            hud.Clear(Color.Black);

            hud.DoomVirtualResolution(() =>
            {
                hud.Image(IntermissionPic, Vec2I.Zero);
                
                DrawAnimations(hud);
                DrawPointer(hud);
                DrawTitle(hud);
                DrawStatistics(hud);
                DrawTime(hud);
            }, ResolutionScale.Center);
        }

        private void DrawAnimations(IHudRenderContext hud)
        {
            if (IntermissionDef == null)
                return;

            foreach (IntermissionAnimation animation in IntermissionDef.Animations)
            {
                if (!animation.ShouldDraw) 
                    continue;
                
                string image = animation.Items[animation.ItemIndex];
                if (!hud.Textures.TryGet(image, out var handle))
                    continue;
                
                Vec2I offset = TranslateDoomOffset(handle.Offset, handle.Dimension); 
                hud.Image(image, animation.Vector + offset);
            }
        }

        private void InitSpots(IHudRenderContext hud)
        {
            if (IntermissionDef == null) 
                return;

            if (!hud.Textures.TryGet(IntermissionDef.Splat, out IRenderableTextureHandle? handle)) 
                return;

            IList<IntermissionSpot> spots = IntermissionDef.Spots;
            Dimension dimension = handle.Dimension;
            Vec2I offset = TranslateDoomOffset(handle.Offset, dimension);

            foreach (var visitedMap in World.GlobalData.VisitedMaps)
            {
                IntermissionSpot? spot = spots.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(visitedMap.MapName));
                if (spot == null) 
                    continue;
                
                m_visitedSpots.Add(spot);
                Vec2I spotOffset = offset + spot.Vector;
                spot.Box = (spotOffset, spotOffset + dimension.Vector);
            }
                    
            m_nextSpot = NextMapInfo == null ? null : spots.FirstOrDefault(x => x.MapName == NextMapInfo.MapName);
            if (m_nextSpot == null || IntermissionDef.Pointer.Count <= 1) 
                return;
            
            m_pointerImage = IntermissionDef.Pointer[0];
            
            if (!hud.Textures.TryGet(m_pointerImage, out IRenderableTextureHandle? pointerHandle)) 
                return;
            
            Dimension nextSpotDimension = pointerHandle.Dimension;
            Vec2I nextSpotOffset = TranslateDoomOffset(pointerHandle.Offset, nextSpotDimension);
            
            nextSpotOffset += m_nextSpot.Vector;
            m_nextSpot.Box = (nextSpotOffset, nextSpotOffset + nextSpotDimension.Vector);
        }
        
        private void DrawPointer(IHudRenderContext hud)
        {
            if (IntermissionState < IntermissionState.NextMap || IntermissionDef == null)
                return;

            if (!m_spotsInit)
            {
                InitSpots(hud);
                m_spotsInit = true;
            }

            if (m_tics - m_lastPointerTic >= (m_drawPointer ? 20 : 11))
            {
                m_drawPointer = !m_drawPointer;
                m_lastPointerTic = m_tics;
            }

            foreach (var visitedSpot in m_visitedSpots)
                hud.Image(IntermissionDef.Splat, visitedSpot.Box.BottomLeft);

            if (m_drawPointer && m_nextSpot != null && m_pointerImage != null)
                hud.Image(m_pointerImage, m_nextSpot.Box.BottomLeft);
        }

        private void DrawTitle(IHudRenderContext hud)
        {
            const string FinishedImage = "WIF";
            const string NowEnteringImage = "WIENTER";
            const int topPaddingY = 4;

            if (IntermissionState >= IntermissionState.NextMap && NextMapInfo != null)
            {
                hud.Image(NowEnteringImage, (0, topPaddingY), out HudBox drawArea, both: Align.TopMiddle);
                hud.Image(NextMapInfo.TitlePatch, (0, drawArea.Height + topPaddingY + 1), both: Align.TopMiddle);
            }
            else
            {
                hud.Image(CurrentMapInfo.TitlePatch, (0, topPaddingY), out HudBox drawArea, both: Align.TopMiddle);
                hud.Image(FinishedImage, (0, drawArea.Height + topPaddingY + 1), both: Align.TopMiddle);
            }
        }

        private void DrawStatistics(IHudRenderContext hud)
        {
            const int LeftOffsetX = 40;
            const int RightOffsetX = 280;
            const int OffsetY = 50;
            const int RowOffsetY = 18;

            if (IntermissionState >= IntermissionState.NextMap)
                return;

            hud.Image("WIOSTK", (LeftOffsetX, OffsetY));
            hud.Image("WIOSTI", (LeftOffsetX, OffsetY + RowOffsetY));
            hud.Image("WISCRT2", (LeftOffsetX, OffsetY + (2 * RowOffsetY)));

            if (IntermissionState >= IntermissionState.TallyingKills)
                DrawNumber(KillPercent, OffsetY);
            
            if (IntermissionState >= IntermissionState.TallyingItems)
                DrawNumber(ItemPercent, OffsetY + RowOffsetY);
            
            if (IntermissionState >= IntermissionState.TallyingSecrets)
                DrawNumber(SecretPercent, OffsetY + (2 * RowOffsetY));

            void DrawNumber(double percent, int offsetY)
            {
                hud.Text($"{percent}%", Font, FontSize, (RightOffsetX, offsetY), anchor: Align.TopRight);
            }
        }

        private void DrawTime(IHudRenderContext hud)
        {
            const int LeftOffsetTimeX = 40;
            const int RightOffsetLevelTimeX = 150;
            const int LeftOffsetParX = 180;
            const int RightOffsetParTimeX = 280;
            const int OffsetY = 40;
            const int TotalOffsetY = 20;
            
            if (IntermissionState >= IntermissionState.NextMap || IntermissionState < IntermissionState.TallyingTime)
                return;
            
            hud.Image("WITIME", (LeftOffsetTimeX, -OffsetY), Align.BottomLeft);
            hud.Image("WIPAR", (LeftOffsetParX, -OffsetY), Align.BottomLeft);
            
            RenderTime(LevelTimeSeconds, RightOffsetLevelTimeX, -OffsetY);
            RenderTime(ParTimeSeconds, RightOffsetParTimeX, -OffsetY);
            
            if (IntermissionState >= IntermissionState.ShowAllStats)
            {
                hud.Image("WIMSTT", (LeftOffsetTimeX, -TotalOffsetY), Align.BottomLeft);

                int seconds = World.GlobalData.TotalTime / (int)Constants.TicksPerSecond;
                RenderTime(seconds, RightOffsetLevelTimeX, -TotalOffsetY);
            }
            
            string GetTimeString(int seconds)
            {
                int minutes = seconds / 60;
                string secondsStr = (seconds % 60).ToString().PadLeft(2, '0');
                return $"{minutes}:{secondsStr}";
            }
            
            void RenderTime(int seconds, int rightOffsetX, int y)
            {
                string levelTime = GetTimeString(seconds);
                hud.Text(levelTime, Font, FontSize, (rightOffsetX, y), window: Align.BottomLeft, anchor: Align.TopRight);
            }
        }
    }
}

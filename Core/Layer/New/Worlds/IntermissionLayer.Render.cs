using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Resources.Definitions.Intermission;
using Helion.Util.Extensions;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Layer.New.Worlds
{
    public partial class IntermissionLayer
    {
        private const string Font = "IntermissionFont";
        private const int FontSize = 12;
        
        private readonly List<IntermissionSpot> m_visitedSpots = new();
        private IntermissionSpot? m_nextSpot;
        private string? m_pointerImage;
        private int m_lastPointerTic;
        private bool m_drawPointer;
        private bool m_spotsInit;
        
        public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
        {
            Font? intermissionFont = World.ArchiveCollection.GetFontDeprecated(Font);
            
            ctx.ClearDepth();
            hud.Clear(Color.Black);
            
            hud.DoomVirtualResolution(() =>
            {
                hud.Image(IntermissionPic);
                
                DrawAnimations(hud);
                DrawPointer(hud);
                DrawTitle(hud);
                DrawStatistics(hud);
                DrawTime(hud);
            });
        }

        private void DrawAnimations(IHudRenderContext hud)
        {
            // TODO
        }

        private void InitSpots(IHudRenderContext hud)
        {
            if (IntermissionDef == null) 
                return;

            if (!hud.Textures.TryGet(IntermissionDef.Splat, out IRenderableTextureHandle? handle)) 
                return;

            IList<IntermissionSpot> spots = IntermissionDef.Spots;
            Dimension dimension = handle.Dimension;
            Vec2I offset = handle.Offset;
            // TODO: draw.TranslateDoomOffset(ref offset, dimension);
                    
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
            Vec2I nextSpotOffset = pointerHandle.Offset;
            // TODO: draw.TranslateDoomOffset(ref nextSpotOffset, nextSpotDimension);
            
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
                hud.Image(IntermissionDef.Splat, origin: visitedSpot.Box.BottomLeft);

            if (m_drawPointer && m_nextSpot != null && m_pointerImage != null)
                hud.Image(m_pointerImage, origin: m_nextSpot.Box.BottomLeft);
        }

        private void DrawTitle(IHudRenderContext hud)
        {
            // TODO
        }

        private void DrawStatistics(IHudRenderContext hud)
        {
            // TODO
        }

        private void DrawTime(IHudRenderContext hud)
        {
            // TODO
        }

/*
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
*/
    }
}

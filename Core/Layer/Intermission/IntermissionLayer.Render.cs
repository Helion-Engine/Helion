using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Intermission;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Resources.Definitions.Intermission;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds;

public partial class IntermissionLayer
{
    private const string MainFont = "IntermissionFont";
    private const string LevelInfoFont = Constants.Fonts.SmallGray;

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

        hud.RenderFullscreenImage(IntermissionPic);

        hud.DoomVirtualResolution(m_renderVirtualIntermissionAction, hud);
    }

    private void RenderVirtualIntermission(IHudRenderContext hud)
    {
        DrawAnimations(hud);
        DrawPointer(hud);
        DrawTitle(hud);
        DrawStatistics(hud);
        DrawTime(hud);
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

            Vec2I offset = TranslateDoomOffset(handle.Offset);
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
        Vec2I offset = TranslateDoomOffset(handle.Offset);

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
        Vec2I nextSpotOffset = TranslateDoomOffset(pointerHandle.Offset);

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
        const int topMargin = 2;

        int offsetY = topMargin;

        if (IntermissionState >= IntermissionState.NextMap && NextMapInfo != null)
        {
            bool isFullscreen = IsFullscreenPatch(hud, NextMapInfo.TitlePatch, m_textUpscalingFactor);

            hud.Image(NowEnteringImage, (0, offsetY) + GetPatchOffset(hud, NowEnteringImage, m_textUpscalingFactor), 
                out HudBox drawArea, both: Align.TopMiddle, upscalingFactor: m_textUpscalingFactor);

            if (!isFullscreen) offsetY += 5 * drawArea.Height / 4;

            DrawMapTitle(hud, NextMapInfo, ref offsetY, m_textUpscalingFactor);
        }
        else
        {
            bool isFullscreen = IsFullscreenPatch(hud, CurrentMapInfo.TitlePatch, m_textUpscalingFactor);
            DrawMapTitle(hud, CurrentMapInfo, ref offsetY, m_textUpscalingFactor);

            if (!isFullscreen)
            {
                hud.Image(FinishedImage, (0, offsetY) + GetPatchOffset(hud, FinishedImage, m_textUpscalingFactor), 
                    both: Align.TopMiddle, upscalingFactor: m_textUpscalingFactor);
            }
        }
    }

    private void DrawMapTitle(IHudRenderContext hud, MapInfoDef mapInfo, ref int offsetY, int textUpscalingFactor)
    {
        if (!string.IsNullOrEmpty(mapInfo.TitlePatch))
        {
            if (hud.Textures.TryGet(mapInfo.TitlePatch, out var handle, upscalingFactor: textUpscalingFactor))
            {
                bool isFullscreen = handle.Dimension.Width >= 320 || handle.Dimension.Height >= 200;
                int drawY = isFullscreen ? 0 : offsetY;

                hud.Image(mapInfo.TitlePatch, (0, drawY) + TranslateDoomOffset(handle.Offset),
                    out HudBox drawArea, both: Align.TopMiddle, upscalingFactor: textUpscalingFactor);

                if (!isFullscreen) offsetY += 5 * drawArea.Height / 4;
            }
            
            return; 
        }

        // TODO would look nicer if there was a large font for the level text
        const int LevelInfoFontSize = 8;

        string name = mapInfo.GetNiceNameOrLookup(m_archiveCollection.Language);
        hud.Text(name, LevelInfoFont, LevelInfoFontSize, (0, offsetY), both: Align.TopMiddle, color: Color.White);
        offsetY += hud.MeasureText(name, LevelInfoFont, LevelInfoFontSize).Height;

        if (mapInfo.Author.Length > 0)
        {
            hud.Text(mapInfo.Author, LevelInfoFont, LevelInfoFontSize, (0, offsetY), both: Align.TopMiddle, color: Color.White);
            offsetY += hud.MeasureText(mapInfo.Author, LevelInfoFont, LevelInfoFontSize).Height + 1;
        }
    }

    private static Vec2I GetPatchOffset(IHudRenderContext hud, string name, int upscalingFactor)
    {
        if (hud.Textures.TryGet(name, out var text, upscalingFactor: upscalingFactor))
            return TranslateDoomOffset(text.Offset);
        return Vec2I.Zero;
    }

    private static bool IsFullscreenPatch(IHudRenderContext hud, string patchName, int upscalingFactor)
    {
        if (string.IsNullOrEmpty(patchName)) return false;

        if (hud.Textures.TryGet(patchName, out var handle, upscalingFactor: upscalingFactor))
            return handle.Dimension.Width >= 320 || handle.Dimension.Height >= 200;
        return false;
    }

    private void DrawStatistics(IHudRenderContext hud)
    {
        const int LeftOffsetX = 50;
        const int RightOffsetX = 280;
        const int OffsetY = 50;
        var fontObject = m_archiveCollection.GetFont(MainFont);
        if (fontObject == null)
            return;

        int RowOffsetY = 3 * fontObject.Get('0').Area.Height / fontObject.UpscalingFactor / 2;

        if (IntermissionState >= IntermissionState.NextMap)
            return;

        hud.Image("WIOSTK", (LeftOffsetX, OffsetY), upscalingFactor: m_textUpscalingFactor);
        hud.Image("WIOSTI", (LeftOffsetX, OffsetY + RowOffsetY), upscalingFactor: m_textUpscalingFactor);
        hud.Image("WISCRT2", (LeftOffsetX, OffsetY + (2 * RowOffsetY)), upscalingFactor: m_textUpscalingFactor);

        if (IntermissionState >= IntermissionState.TallyingKills)
            DrawNumber(KillPercent, OffsetY);

        if (IntermissionState >= IntermissionState.TallyingItems)
            DrawNumber(ItemPercent, OffsetY + RowOffsetY);

        if (IntermissionState >= IntermissionState.TallyingSecrets)
            DrawNumber(SecretPercent, OffsetY + (2 * RowOffsetY));

        void DrawNumber(double percent, int offsetY)
        {
            int fontSize = hud.GetFontMaxHeight(MainFont);
            hud.Text($"{percent}%", MainFont, fontSize, (RightOffsetX, offsetY), anchor: Align.TopRight);
        }
    }

    private void DrawTime(IHudRenderContext hud)
    {
        const int LeftOffsetTimeX = 8;
        const int RightOffsetLevelTimeX = 150;
        const int LeftOffsetParX = 168;
        const int OffsetY = 40;
        const int TotalOffsetY = 20;

        if (IntermissionState >= IntermissionState.NextMap || IntermissionState < IntermissionState.TallyingTime)
            return;

        hud.Image("WITIME", (LeftOffsetTimeX, -OffsetY), Align.BottomLeft, upscalingFactor: m_textUpscalingFactor);
        RenderTime(hud, LevelTimeSeconds, RightOffsetLevelTimeX, -OffsetY);

        if (ParTimeSeconds != 0)
        {
            hud.Image("WIPAR", (LeftOffsetParX, -OffsetY), Align.BottomLeft, upscalingFactor: m_textUpscalingFactor);
            RenderTime(hud, ParTimeSeconds, 320 - LeftOffsetTimeX, -OffsetY);
        }

        if (IntermissionState >= IntermissionState.ShowAllStats)
        {
            hud.Image("WIMSTT", (LeftOffsetTimeX, -TotalOffsetY), Align.BottomLeft, upscalingFactor: m_textUpscalingFactor);

            int seconds = World.GlobalData.TotalTime / (int)Constants.TicksPerSecond;
            RenderTime(hud, seconds, RightOffsetLevelTimeX, -TotalOffsetY);
        }
    }

    static string GetTimeString(int seconds)
    {
        int minutes = seconds / 60;
        string secondsStr = (seconds % 60).ToString(CultureInfo.CurrentCulture).PadLeft(2, '0');
        return $"{minutes}:{secondsStr}";
    }

    void RenderTime(IHudRenderContext hud, int seconds, int rightOffsetX, int y)
    {
        string levelTime = GetTimeString(seconds);
        int fontSize = hud.GetFontMaxHeight(MainFont);
        hud.Text(levelTime, MainFont, fontSize, (rightOffsetX, y), window: Align.BottomLeft, anchor: Align.TopRight);
    }
}